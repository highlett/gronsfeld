const API_URL = "http://localhost:5161";

const loginBtn = document.getElementById("loginBtn");
const registerBtn = document.getElementById("registerBtn");
const message = document.getElementById("message");

document.addEventListener("DOMContentLoaded", () => {
    if (localStorage.getItem("userId")) {
        window.location.replace("dashboard.html");
        return;
    }

    const loginBtn = document.getElementById("loginBtn");
    const registerBtn = document.getElementById("registerBtn");
    const message = document.getElementById("message");

    loginBtn.onclick = login;
    registerBtn.onclick = register;
});




loginBtn.addEventListener("click", login);
registerBtn.addEventListener("click", register);

async function login() {
    const username = document.getElementById("username").value;
    const password = document.getElementById("password").value;

    // ✅ ПРОВЕРКА ТУТ
    if (!username || !password) {
        message.style.color = "red";
        message.textContent = "Введите логин и пароль";
        return;
    }

    const response = await fetch(`${API_URL}/login`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify({ username, password })
    });

    if (!response.ok) {
        message.style.color = "red";
        message.textContent = "Ошибка входа";
        return;
    }

    const data = await response.json();

    localStorage.setItem("userId", data.userId);

    message.style.color = "#80ff80";
    message.textContent = "Вход успешен!";

    window.location.href = "dashboard.html";
}

async function register() {
    const username = document.getElementById("username").value;
    const password = document.getElementById("password").value;

    // ✅ ПРОВЕРКА ТУТ
    if (!username || !password) {
        message.style.color = "red";
        message.textContent = "Введите логин и пароль";
        return;
    }

    const response = await fetch(`${API_URL}/register`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify({ username, password })
    });

    if (!response.ok) {
        const text = await response.text();
        message.style.color = "red";
        message.textContent = text;
        return;
    }

    message.style.color = "#80ff80";
    message.textContent = "Регистрация успешна!";
}
