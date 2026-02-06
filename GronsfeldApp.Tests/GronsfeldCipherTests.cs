using Xunit;

public class GronsfeldCipherTests
{
    [Fact]
    public void Encrypt_EnglishText_ReturnsCorrectResult()
    {
        string result = GronsfeldCipher.Encrypt("HELLO", "123");
        Assert.Equal("IGOMQ", result);
    }

    [Fact]
    public void Decrypt_EnglishText_ReturnsOriginal()
    {
        string result = GronsfeldCipher.Decrypt("IGOMQ", "123");
        Assert.Equal("HELLO", result);
    }

    [Fact]
    public void Encrypt_CyrillicText_ReturnsCorrectResult()
    {
        // П(16)+1=Р, Р(17)+2=Т, И(9)+3=Л, В(2)+1=Г, Е(5)+2=Ж, Т(19)+3=Х
        string result = GronsfeldCipher.Encrypt("ПРИВЕТ", "123");
        Assert.Equal("РТЛГЖХ", result); // ПРАВИЛЬНОЕ значение!
    }

    [Fact]
    public void Decrypt_CyrillicText_ReturnsOriginal()
    {
        string result = GronsfeldCipher.Decrypt("РТЛГЖХ", "123");
        Assert.Equal("ПРИВЕТ", result);
    }

    [Fact]
    public void RoundTrip_EnglishText_ReturnsOriginal()
    {
        string original = "SECRET";
        string key = "314";
        string encrypted = GronsfeldCipher.Encrypt(original, key);
        string decrypted = GronsfeldCipher.Decrypt(encrypted, key);
        Assert.Equal(original, decrypted);
    }

    [Fact]
    public void RoundTrip_CyrillicText_ReturnsOriginal()
    {
        string original = "ШИФР";
        string key = "271";
        string encrypted = GronsfeldCipher.Encrypt(original, key);
        string decrypted = GronsfeldCipher.Decrypt(encrypted, key);
        Assert.Equal(original, decrypted);
    }

    [Fact]
    public void Encrypt_PreservesNonAlphabetCharacters()
    {
        // Ключ применяется позиционно ко ВСЕЙ строке (включая пробелы и знаки)
        // H(7)+1=I, e(4)+2=G, l(11)+3=O, l(11)+1=M, o(14)+2=Q, ,(skip), (space,skip), W(22)+2=Y...
        string result = GronsfeldCipher.Encrypt("Hello, World! 123", "123");
        Assert.Equal("IGOMQ, YRSNG! 123", result); // ПРАВИЛЬНОЕ значение!
    }

    [Fact]
    public void Encrypt_WrapAroundZ_ReturnsA()
    {
        string result = GronsfeldCipher.Encrypt("Z", "1");
        Assert.Equal("A", result);
    }

    [Fact]
    public void Encrypt_WrapAroundЯ_ReturnsА()
    {
        string result = GronsfeldCipher.Encrypt("Я", "1");
        Assert.Equal("А", result);
    }

    [Fact]
    public void Encrypt_KeyZero_NoChange()
    {
        string result = GronsfeldCipher.Encrypt("TEST", "0");
        Assert.Equal("TEST", result);
    }
}