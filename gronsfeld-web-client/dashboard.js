

const API_URL = "http://localhost:5161";

document.addEventListener("DOMContentLoaded", () => {

    /* ========== АВТОРИЗАЦИЯ ========== */

    const userId = localStorage.getItem("userId");
    if (!userId) {
        window.location.replace("index.html");
        return;
    }

    /* ========== ОСНОВНЫЕ ЭЛЕМЕНТЫ ========== */

    const content = document.getElementById("content");

    document.getElementById("encryptBtn").onclick = () => {
    setActiveButton("encryptBtn");
    switchScreen(showEncrypt);
};

document.getElementById("decryptBtn").onclick = () => {
    setActiveButton("decryptBtn");
    switchScreen(showDecrypt);
};

document.getElementById("textsBtn").onclick = () => {
    setActiveButton("textsBtn");
    switchScreen(showTexts);
};

document.getElementById("historyBtn").onclick = () => {
    setActiveButton("historyBtn");
    switchScreen(showHistory);
};

document.getElementById("statsBtn").onclick = () => {
    setActiveButton("statsBtn");
    switchScreen(showStats);
};

document.getElementById("changePasswordBtn").onclick = () => {
    setActiveButton("changePasswordBtn");
    switchScreen(showChangePassword);
};


document.getElementById("logoutBtn").onclick = logout;

function setActiveButton(buttonId) {
    document.querySelectorAll(".menu-btn").forEach(btn => {
        btn.classList.remove("active");
    });

    const activeBtn = document.getElementById(buttonId);
    if (activeBtn) {
        activeBtn.classList.add("active");
    }
}


    let isSwitching = false;

    function switchScreen(renderFn) {
        if (isSwitching) return;
        isSwitching = true;

        const content = document.getElementById("content");

        content.classList.add("fade-out");

        setTimeout(() => {
            renderFn();                 // showEncrypt / showHistory / ...
            content.classList.remove("fade-out");
            isSwitching = false;
        }, 250);
    }
    // экран по умолчанию
    showEncrypt();

    /* ========== ЭКРАНЫ ========== */

    

    function showEncrypt() {
        content.innerHTML = `
            <h3>Шифрование (Гронсфельд)</h3>

            <textarea id="plainText" rows="4"
                placeholder="Введите текст"
                style="width:100%"></textarea>

            <input id="key" type="text"
                placeholder="Ключ (цифры)"
                style="margin-top:10px; width:100%">

            <button id="encryptSubmit" style="margin-top:10px">
                Зашифровать
            </button>

            <h4>Результат:</h4>
            <pre id="result"></pre>
        `;

        document.getElementById("encryptSubmit").onclick = encryptRequest;
    }

    function showDecrypt() {
        content.innerHTML = `
            <h3>Расшифрование (Гронсфельд)</h3>

            <textarea id="cipherText" rows="4"
                placeholder="Введите зашифрованный текст"
                style="width:100%"></textarea>

            <input id="key" type="text"
                placeholder="Ключ (цифры)"
                style="margin-top:10px; width:100%">

            <button id="decryptSubmit" style="margin-top:10px">
                Расшифровать
            </button>

            <h4>Результат:</h4>
            <pre id="result"></pre>
        `;

        document.getElementById("decryptSubmit").onclick = decryptRequest;
    }

function validateKeyInput() {
    const keyInput = document.getElementById("key");

    keyInput.addEventListener("input", () => {
        if (!/^\d*$/.test(keyInput.value)) {
            keyInput.value = keyInput.value.replace(/\D/g, "");
            alert("Ключ должен содержать только цифры");
        }
    });
}

document.getElementById("encryptSubmit").onclick = encryptRequest;
validateKeyInput();

document.getElementById("decryptSubmit").onclick = decryptRequest;
validateKeyInput();



    function showTexts() {
    content.innerHTML = `
        <h3>Мои тексты</h3>

        <textarea id="newText" rows="3"
            placeholder="Введите текст для сохранения"
            style="width:100%"></textarea>

        <button id="saveTextBtn" style="margin-top:10px">
            💾 Сохранить текст
        </button>

        <h4 style="margin-top:20px">Сохранённые тексты:</h4>
        <ul id="textsList"></ul>
    `;

    document.getElementById("saveTextBtn").onclick = saveText;
    loadTexts();
}

async function loadTexts() {
    const list = document.getElementById("textsList");
    list.innerHTML = "Загрузка...";

    try {
        const response = await fetch(
            `${API_URL}/users/${userId}/texts`
        );

        if (!response.ok) {
            list.innerHTML = "Ошибка загрузки";
            return;
        }

        const texts = await response.json();

        if (texts.length === 0) {
            list.innerHTML = "<li>Текстов пока нет</li>";
            return;
        }

        list.innerHTML = "";

        texts.forEach((t, index) => {
            const li = document.createElement("li");
            li.classList.add("list-item");

            // задержка появления
            li.style.animationDelay = `${index * 60}ms`;
            li.style.marginBottom = "15px";

            const pre = document.createElement("pre");
            pre.textContent = t.content;

            const editBtn = document.createElement("button");
            editBtn.textContent = "✏️ Редактировать";
            editBtn.style.marginRight = "5px";

            const deleteBtn = document.createElement("button");
            deleteBtn.textContent = "🗑 Удалить";

            editBtn.onclick = () => startEditText(t.id, t.content, li);
            deleteBtn.onclick = () => deleteText(t.id);

            li.appendChild(pre);
            li.appendChild(editBtn);
            li.appendChild(deleteBtn);

            list.appendChild(li);
        });



    } catch {
        list.innerHTML = "Нет соединения с сервером";
    }
}

async function saveText() {
    const text = document.getElementById("newText").value;

    if (!text) {
        alert("Введите текст");
        return;
    }

    try {
        const response = await fetch(
            `${API_URL}/users/${userId}/texts`,
            {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ text })
            }
        );

        if (!response.ok) {
            alert("Ошибка сохранения");
            return;
        }

        document.getElementById("newText").value = "";
        loadTexts();

    } catch {
        alert("Нет соединения с сервером");
    }
}

async function deleteText(textId) {
    if (!confirm("Удалить этот текст?")) return;

    try {
        const response = await fetch(
            `${API_URL}/users/${userId}/texts/${textId}`,
            { method: "DELETE" }
        );

        if (!response.ok) {
            alert("Ошибка удаления");
            return;
        }

        loadTexts();

    } catch {
        alert("Нет соединения с сервером");
    }
}

function startEditText(textId, oldContent, li) {
    li.innerHTML = "";

    const textarea = document.createElement("textarea");
    textarea.value = oldContent;
    textarea.style.width = "100%";
    textarea.rows = 4;

    const saveBtn = document.createElement("button");
    saveBtn.textContent = "💾 Сохранить";
    saveBtn.style.marginRight = "5px";

    const cancelBtn = document.createElement("button");
    cancelBtn.textContent = "❌ Отмена";

    saveBtn.onclick = () => updateText(textId, textarea.value);
    cancelBtn.onclick = loadTexts;

    li.appendChild(textarea);
    li.appendChild(saveBtn);
    li.appendChild(cancelBtn);
}

async function updateText(textId, newText) {
    if (!newText) {
        alert("Текст не может быть пустым");
        return;
    }

    try {
        const response = await fetch(
            `${API_URL}/users/${userId}/texts/${textId}`,
            {
                method: "PATCH",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ text: newText })
            }
        );

        if (!response.ok) {
            alert("Ошибка обновления текста");
            return;
        }

        loadTexts();

    } catch {
        alert("Нет соединения с сервером");
    }
}




    function showHistory() {
    content.innerHTML = `
        <h3>История запросов</h3>

        <button id="clearHistoryBtn"
            style="margin-bottom:15px; background:#c0392b; color:white">
            🗑 Очистить историю
        </button>

        <ul id="historyList"></ul>
    `;

    document.getElementById("clearHistoryBtn").onclick = clearHistory;
    loadHistory();
    }
    
    async function loadHistory() {
    const list = document.getElementById("historyList");
    list.innerHTML = "Загрузка...";

    try {
        const response = await fetch(
            `${API_URL}/users/${userId}/history`
        );

        if (!response.ok) {
            list.innerHTML = "Ошибка загрузки истории";
            return;
        }

        const history = await response.json();

        if (history.length === 0) {
            list.innerHTML = "<li>История пуста</li>";
            return;
        }

        list.innerHTML = "";

        history.reverse().forEach((h, index) => {
            const li = document.createElement("li");
        li.classList.add("list-item");

        li.style.animationDelay = `${index * 60}ms`;

            li.style.marginBottom = "15px";

            const icon = h.action === "encrypt" ? "🔐" : "🔓";

            li.innerHTML = `
                <strong>${icon} ${h.action.toUpperCase()}</strong><br>
                <small>${new Date(h.timestamp).toLocaleString()}</small>
                <pre>Текст: ${h.text}</pre>
                <pre>Результат: ${h.result}</pre>
                <hr>
            `;

            list.appendChild(li);
        });

    } catch {
        list.innerHTML = "Нет соединения с сервером";
    }
    }

    async function clearHistory() {
    if (!confirm("Удалить всю историю запросов?")) return;

    try {
        const response = await fetch(
            `${API_URL}/users/${userId}/history`,
            { method: "DELETE" }
        );

        if (!response.ok) {
            alert("Ошибка очистки истории");
            return;
        }

        loadHistory();

    } catch {
        alert("Нет соединения с сервером");
    }
    }



    function showStats() {
    content.innerHTML = "<h3>Статистика</h3><p>Загрузка...</p>";
    loadStats();
}

async function loadStats() {
    try {
        const response = await fetch(
            `${API_URL}/users/${userId}/statistics`
        );

        if (!response.ok) {
            content.innerHTML = "<p>Ошибка загрузки статистики</p>";
            return;
        }

        const s = await response.json();

        content.innerHTML = `
            <h3>📊 Статистика пользователя</h3>

            <p><strong>Пользователь:</strong> ${s.username}</p>

            <ul>
                <li>🔐 Шифрований: <strong>${s.encryptCount}</strong></li>
                <li>🔓 Расшифрований: <strong>${s.decryptCount}</strong></li>
                <li>📄 Сохранённых текстов: <strong>${s.savedTexts}</strong></li>
            </ul>

            <p><strong>Последнее действие:</strong> ${s.lastAction}</p>
            <p><strong>Время:</strong> ${s.lastTime}</p>
        `;
    } catch {
        content.innerHTML = "<p>Нет соединения с сервером</p>";
    }
}

function showChangePassword() {
    content.innerHTML = `
        <h3>🔑 Смена пароля</h3>

        <input id="oldPassword" type="password"
            placeholder="Старый пароль"
            style="width:100%; margin-bottom:10px">

        <input id="newPassword" type="password"
            placeholder="Новый пароль"
            style="width:100%; margin-bottom:10px">

        <button id="savePasswordBtn">💾 Сохранить</button>
        <button id="cancelBtn">❌ Отмена</button>

        <p id="passwordMessage"></p>
    `;

    document.getElementById("savePasswordBtn").onclick = changePassword;
    document.getElementById("cancelBtn").onclick = showStats;
}

async function changePassword() {
    const oldPassword = document.getElementById("oldPassword").value;
    const newPassword = document.getElementById("newPassword").value;
    const msg = document.getElementById("passwordMessage");

    if (!oldPassword || !newPassword) {
        msg.style.color = "red";
        msg.textContent = "Заполните все поля";
        return;
    }

    try {
        const response = await fetch(
            `${API_URL}/users/${userId}/password`,
            {
                method: "PATCH",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ oldPassword, newPassword })
            }
        );

        const text = await response.text();

        if (!response.ok) {
            msg.style.color = "red";
            msg.textContent = text;
            return;
        }

        msg.style.color = "green";
        msg.textContent = "Пароль успешно изменён";

        setTimeout(showStats, 1500);

    } catch {
        msg.style.color = "red";
        msg.textContent = "Нет соединения с сервером";
    }
}

function switchScreen(renderFn) {
    const content = document.getElementById("content");

    // 1. запускаем анимацию исчезновения
    content.classList.add("fade-out");

    // 2. ждём пока анимация закончится
    setTimeout(() => {
        renderFn(); // ← showEncrypt / showTexts / ...
        content.classList.remove("fade-out");
    }, 250); // совпадает с CSS transition
}




    function logout() {
        localStorage.removeItem("userId");
        window.location.replace("index.html");
    }

    /* ========== API ========== */

    async function encryptRequest() {
        const text = document.getElementById("plainText").value;
        const key = document.getElementById("key").value;
        const result = document.getElementById("result");

        if (!text || !key) {
            result.textContent = "Введите текст и ключ";
            return;
        }

        if (!/^\d+$/.test(key)) {
    result.textContent = "Ключ должен состоять только из цифр";
    return;
}


        try {
            const response = await fetch(
                `${API_URL}/users/${userId}/encrypt`,
                {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ text, key })
                }
            );

            if (!response.ok) {
                result.textContent = "Ошибка сервера";
                return;
            }

            const data = await response.json();
            result.textContent = data.encryptedText;

        } catch {
            result.textContent = "Нет соединения с сервером";
        }
    }

    async function decryptRequest() {
        const text = document.getElementById("cipherText").value;
        const key = document.getElementById("key").value;
        const result = document.getElementById("result");



        if (!text || !key) {
            result.textContent = "Введите текст и ключ";
            return;
        }

        if (!/^\d+$/.test(key)) {
    result.textContent = "Ключ должен состоять только из цифр";
    return;
}


        try {
            const response = await fetch(
                `${API_URL}/users/${userId}/decrypt`,
                {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ text, key })
                }
            );

            if (!response.ok) {
                result.textContent = "Ошибка сервера";
                return;
            }

            const data = await response.json();
            result.textContent = data.decryptedText;

        } catch {
            result.textContent = "Нет соединения с сервером";
        }
    }

});


