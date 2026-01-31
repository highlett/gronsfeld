using System.Text;
using System.Text.Json;
using System.Security.Cryptography;


// Создание приложения
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();
var app = builder.Build();
app.UseCors(policy =>
    policy
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()
);


// ========== ЭНДПОИНТЫ ==========

// Регистрация POST
app.MapPost("/register", (AuthRequest request) =>
{
    if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
        return Results.BadRequest("Username и password обязательны");

    if (request.Username.Length < 3 || request.Username.Length > 20)
        return Results.BadRequest("Имя пользователя должно содержать от 3 до 20 символов");
    if (request.Password.Length < 4 || request.Password.Length > 30)
        return Results.BadRequest("Пароль должен содержать от 4 до 30 символов");

    if (!request.Username.All(char.IsLetterOrDigit))
        return Results.BadRequest("Имя пользователя может содержать только буквы и цифры");
    if (!request.Password.All(char.IsLetterOrDigit))
        return Results.BadRequest("«Пароль должен состоять только из букв и цифр (без спецсимволов)»");
    if (!request.Password.Any(char.IsDigit))
        return Results.BadRequest("Пароль должен содержать хотя бы одну цифру");
    if (!request.Password.Any(char.IsLetter))
        return Results.BadRequest("Пароль должен содержать хотя бы одну букву");


    var data = Storage.LoadData();

    if (data.Users.Any(u => u.Username == request.Username))
        return Results.BadRequest("Пользователь с таким username уже существует");

    var user = new User
    {
        Id = data.NextUserId++,
        Username = request.Username,
        Password = PasswordHasher.Hash(request.Password),
        NextTextId = 1
    };

    data.Users.Add(user);
    Storage.SaveData(data);

    return Results.Ok(new
    {
        message = "Пользователь успешно зарегистрирован",
        userId = user.Id,
        username = user.Username
    });
});



// Авторизация POST

app.MapPost("/login", (AuthRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        return Results.BadRequest("Имя пользователя и пароль обязательны");

    if (!request.Username.All(char.IsLetterOrDigit))
        return Results.BadRequest("Имя пользователя может содержать только буквы и цифры");
    if (!request.Password.All(char.IsLetterOrDigit))
        return Results.BadRequest("Пароль может содержать только буквы и цифры");

    var data = Storage.LoadData();

    // Хэшируем присланный пароль
    var hashedPassword = PasswordHasher.Hash(request.Password);

    // Ищем пользователя с этим username и хэшем пароля
    var user = data.Users.FirstOrDefault(u =>
        u.Username == request.Username && u.Password == hashedPassword);

    if (user == null)
        return Results.Unauthorized();

    return Results.Ok(new
    {
        message = "Авторизация успешна",
        userId = user.Id,
        username = user.Username
    });
});


// Смена пароля PATCH
app.MapPatch("/users/{userId}/password", (int userId, UserPatchRequest request) =>
{
    if (string.IsNullOrEmpty(request.OldPassword))
        return Results.BadRequest("Старый пароль обязателен");
    
    if (string.IsNullOrEmpty(request.NewPassword))
        return Results.BadRequest("Новый пароль обязателен");

    if (request.NewPassword.Length < 4 || request.NewPassword.Length > 30)
        return Results.BadRequest("Пароль должен содержать от 4 до 30 символов");

    if (!request.NewPassword.All(char.IsLetterOrDigit))
        return Results.BadRequest("Пароль может содержать только буквы и цифры");

    var data = Storage.LoadData();
    var user = data.Users.FirstOrDefault(u => u.Id == userId);
    if (user == null)
        return Results.NotFound($"Пользователь с ID {userId} не найден");

    var oldPasswordHash = PasswordHasher.Hash(request.OldPassword);

    if (user.Password != oldPasswordHash)
        return Results.BadRequest("Старый пароль неверен");

    user.Password = PasswordHasher.Hash(request.NewPassword);


    Storage.SaveData(data);

    return Results.Ok(new { message = "Пароль успешно обновлен" });
});

// Изменить Текст PATCH
app.MapPatch("/users/{userId}/texts/{textId}", (int userId, int textId, TextRequest request) =>
{
    if (string.IsNullOrEmpty(request.Text))
        return Results.BadRequest("Текст не может быть пустым");

    var data = Storage.LoadData();
    var user = data.Users.FirstOrDefault(u => u.Id == userId);
    if (user == null)
        return Results.NotFound($"Пользователь с ID {userId} не найден");

    var text = user.Texts.FirstOrDefault(t => t.Id == textId);
    if (text == null)
        return Results.NotFound($"Текст с ID {textId} не найден у пользователя {userId}");

    text.Content = request.Text;
    Storage.SaveData(data);

    return Results.Ok(new { message = $"Текст с ID {textId} обновлён", newContent = text.Content });
});

// Добавить текст POST
app.MapPost("/users/{userId}/texts", (int userId, TextRequest request) =>
{
    var data = Storage.LoadData();
    var user = data.Users.FirstOrDefault(u => u.Id == userId);

    if (user == null)
        return Results.NotFound($"Пользователь с ID {userId} не найден");

    var textItem = new TextItem
    {
        Id = user.NextTextId++,
        Content = request.Text
    };

    user.Texts.Add(textItem);
    Storage.SaveData(data);

    return Results.Ok(new
    {
        id = textItem.Id,
        text = textItem.Content
    });
});

// Получить текст GET
app.MapGet("/users/{userId}/texts/{textId}", (int userId, int textId) =>
{
    var data = Storage.LoadData();
    var user = data.Users.FirstOrDefault(u => u.Id == userId);
    if (user == null)
        return Results.NotFound($"Пользователь с ID {userId} не найден");

    var text = user.Texts.FirstOrDefault(t => t.Id == textId);
    if (text == null)
        return Results.NotFound($"Текст с ID {textId} не найден у пользователя {userId}");

    return Results.Ok(text);
});

// Получить тексты GET
app.MapGet("/users/{userId}/texts", (int userId) =>
{
    var data = Storage.LoadData();
    var user = data.Users.FirstOrDefault(u => u.Id == userId);

    if (user == null)
        return Results.NotFound($"Пользователь с ID {userId} не найден");

    return Results.Ok(user.Texts);
});

// Удалить текст
app.MapDelete("/users/{userId}/texts/{textId}", (int userId, int textId) =>
{
    var data = Storage.LoadData();
    var user = data.Users.FirstOrDefault(u => u.Id == userId);

    if (user == null)
        return Results.NotFound($"Пользователь с ID {userId} не найден");

    var text = user.Texts.FirstOrDefault(t => t.Id == textId);
    if (text == null)
        return Results.NotFound($"Текст с ID {textId} не найден у пользователя {userId}");

    user.Texts.Remove(text);
    Storage.SaveData(data);

    return Results.Ok($"Текст с ID {textId} удален у пользователя {userId}");
});


// Удалить пользователя
app.MapDelete("/users/{userId}", (int userId) =>
{
    var data = Storage.LoadData();
    var user = data.Users.FirstOrDefault(u => u.Id == userId);

    if (user == null)
        return Results.NotFound($"Пользователь с ID {userId} не найден");

    data.Users.Remove(user);
    Storage.SaveData(data);

    return Results.Ok(new
    {
        message = $"Пользователь {user.Username} и все его тексты удалены"
    });
});


// Шифрование (Гронсфельд) для конкретного пользователя
app.MapPost("/users/{userId}/encrypt", (int userId, GronsfeldRequest request) =>
{
    if (string.IsNullOrEmpty(request.Text) || string.IsNullOrEmpty(request.Key))
        return Results.BadRequest("Text и Key обязательны");

    var data = Storage.LoadData();
    var user = data.Users.FirstOrDefault(u => u.Id == userId);

    if (user == null)
        return Results.NotFound($"Пользователь с ID {userId} не найден");

    var result = GronsfeldCipher.Encrypt(request.Text, request.Key);

    // Сохраняем в историю конкретного пользователя
    data.History.Add(new HistoryItem
    {
        UserId = user.Id,
        Action = "encrypt",
        Text = request.Text,
        Result = result,
        Timestamp = DateTime.Now
    });

    Storage.SaveData(data);

    return Results.Ok(new
    {
        userId = user.Id,
        originalText = request.Text,
        encryptedText = result,
    });
});


// Расшифрование (Гронсфельд) для конкретного пользователя
app.MapPost("/users/{userId}/decrypt", (int userId, GronsfeldRequest request) =>
{
    if (string.IsNullOrEmpty(request.Text) || string.IsNullOrEmpty(request.Key))
        return Results.BadRequest("Text и Key обязательны");

    var data = Storage.LoadData();
    var user = data.Users.FirstOrDefault(u => u.Id == userId);

    if (user == null)
        return Results.NotFound($"Пользователь с ID {userId} не найден");

    var result = GronsfeldCipher.Decrypt(request.Text, request.Key);

    // Сохраняем в историю конкретного пользователя
    data.History.Add(new HistoryItem
    {
        UserId = user.Id,
        Action = "decrypt",
        Text = request.Text,
        Result = result,
        Timestamp = DateTime.Now
    });

    Storage.SaveData(data);

    return Results.Ok(new
    {
        userId = user.Id,
        encryptedText = request.Text,
        decryptedText = result,
    });
});


// История пользователя GET
app.MapGet("/users/{userId}/history", (int userId) =>
{
    var data = Storage.LoadData();
    var user = data.Users.FirstOrDefault(u => u.Id == userId);
    if (user == null)
        return Results.NotFound($"Пользователь с ID {userId} не найден");

    var userHistory = data.History.Where(h => h.UserId == userId).ToList();
    return Results.Ok(userHistory);
});

// Удаление истории запросов пользователя Delete
app.MapDelete("/users/{userId}/history", (int userId) =>
{
    var data = Storage.LoadData();
    var user = data.Users.FirstOrDefault(u => u.Id == userId);
    if (user == null)
        return Results.NotFound($"Пользователь с ID {userId} не найден");

    data.History.RemoveAll(h => h.UserId == userId);
    Storage.SaveData(data);

    return Results.Ok(new { message = $"История пользователя {user.Username} удалена" });
});



// Статистика конкретного пользователя
app.MapGet("/users/{userId}/statistics", (int userId) =>
{
    var data = Storage.LoadData();
    var user = data.Users.FirstOrDefault(u => u.Id == userId);

    if (user == null)
        return Results.NotFound($"Пользователь с ID {userId} не найден");

    // Фильтруем историю по userId
    var userHistory = data.History.Where(h => h.UserId == userId).ToList();

    int encryptCount = userHistory.Count(h => h.Action == "encrypt");
    int decryptCount = userHistory.Count(h => h.Action == "decrypt");
    int savedTexts = user.Texts.Count;
    string lastAction = userHistory.LastOrDefault()?.Action ?? "-";
    string lastTime = userHistory.LastOrDefault()?.Timestamp.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";

    return Results.Ok(new
    {
        username = user.Username,
        encryptCount,
        decryptCount,
        savedTexts,
        lastAction,
        lastTime
    });
});



// Главная страница
app.MapGet("/", () => "GronsfeldApp API работает! Используйте /register, /login, /encrypt, /decrypt, /statistics.");

app.Run();


// ========== КЛАССЫ ==========

public record AuthRequest(string Username, string Password);
public record TextRequest(string Text);
public record GronsfeldRequest(string Text, string Key);
public record UserPatchRequest(string? OldPassword, string? NewPassword);



public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public int NextTextId { get; set; } = 1;
    public List<TextItem> Texts { get; set; } = new();
}

public class TextItem
{
    public int Id { get; set; }
    public string Content { get; set; } = "";
}

public class HistoryItem
{
    public int UserId { get; set; }
    public string Action { get; set; } = "";
    public string Text { get; set; } = "";
    public string Result { get; set; } = "";
    public DateTime Timestamp { get; set; }
}


public class Data
{
    public List<User> Users { get; set; } = new();
    public List<HistoryItem> History { get; set; } = new(); // Что это за строчка?
    public int NextUserId { get; set; } = 1;
}

public static class Storage
{
    private static readonly string DataFile = "data.json";
    private static readonly object Lock = new();

    public static Data LoadData()
    {
        lock (Lock)
        {
            if (!File.Exists(DataFile))
                return new Data();

            try
            {
                var json = File.ReadAllText(DataFile);
                var data = JsonSerializer.Deserialize<Data>(json) ?? new Data();

                if (data.Users.Count > 0)
                    data.NextUserId = data.Users.Max(u => u.Id) + 1;

                return data;
            }
            catch
            {
                return new Data();
            }
        }
    }

    public static void SaveData(Data data)
    {
        lock (Lock)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(data, options);
            File.WriteAllText(DataFile, json);
        }
    }
}

// ========== ШИФР ГРОНСФЕЛЬДА ========== :

public static class GronsfeldCipher
{
    private const string LatinAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string CyrillicAlphabet = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ";

    public static string Encrypt(string text, string key)
    {
        var result = new StringBuilder();
        var keyDigits = key.Select(c => c - '0').ToArray();

        for (int i = 0; i < text.Length; i++)
        {
            char c = char.ToUpper(text[i]);
            int shift = keyDigits[i % keyDigits.Length];
            
            if (LatinAlphabet.Contains(c))
            {
                int index = LatinAlphabet.IndexOf(c);
                int newIndex = (index + shift) % LatinAlphabet.Length;
                result.Append(LatinAlphabet[newIndex]);
            }
            else if (CyrillicAlphabet.Contains(c))
            {
                int index = CyrillicAlphabet.IndexOf(c);
                int newIndex = (index + shift) % CyrillicAlphabet.Length;
                result.Append(CyrillicAlphabet[newIndex]);
            }
            else
            {
                result.Append(c);
            }
        }
        return result.ToString();
    }

    public static string Decrypt(string text, string key)
    {
        var result = new StringBuilder();
        var keyDigits = key.Select(c => c - '0').ToArray();

        for (int i = 0; i < text.Length; i++)
        {
            char c = char.ToUpper(text[i]);
            int shift = keyDigits[i % keyDigits.Length];
            
            if (LatinAlphabet.Contains(c))
            {
                int index = LatinAlphabet.IndexOf(c);
                int newIndex = (index - shift + LatinAlphabet.Length) % LatinAlphabet.Length;
                result.Append(LatinAlphabet[newIndex]);
            }
            else if (CyrillicAlphabet.Contains(c))
            {
                int index = CyrillicAlphabet.IndexOf(c);
                int newIndex = (index - shift + CyrillicAlphabet.Length) % CyrillicAlphabet.Length;
                result.Append(CyrillicAlphabet[newIndex]);
            }
            else
            {
                result.Append(c);
            }
        }
        return result.ToString();
    }
}


// Ф-ия хэширования 
public static class PasswordHasher
{
    public static string Hash(string input)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}