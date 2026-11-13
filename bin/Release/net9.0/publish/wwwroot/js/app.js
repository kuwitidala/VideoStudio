const API_URL = "";

const STATUS_NAMES = {
    Pending: "Ожидает подтверждения",
    Confirmed: "Подтверждена",
    Rejected: "Отклонена",
    Cancelled: "Отменена"
};

const IDENTITY_ERRORS = {
    DuplicateUserName: "Пользователь с таким email уже зарегистрирован.",
    DuplicateEmail: "Пользователь с таким email уже зарегистрирован.",
    PasswordTooShort: "Пароль слишком короткий.",
    PasswordRequiresNonAlphanumeric: "В пароле нужен хотя бы один спецсимвол (например, !).",
    PasswordRequiresDigit: "В пароле нужна хотя бы одна цифра.",
    PasswordRequiresLower: "В пароле нужна строчная буква.",
    PasswordRequiresUpper: "В пароле нужна заглавная буква.",
    PasswordMismatch: "Текущий пароль указан неверно."
};

function $(selector, root) {
    return (root || document).querySelector(selector);
}

function $$(selector, root) {
    return Array.from((root || document).querySelectorAll(selector));
}

function esc(value) {
    return String(value === null || value === undefined ? "" : value)
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;");
}

function safeUrl(url) {
    return /^(https?:\/\/|\/)/i.test(url || "") ? url : "";
}

function money(value) {
    return Number(value).toLocaleString("ru-RU") + " ₽";
}

function parseDate(value) {
    const text = String(value);
    return new Date(/(Z|[+-]\d\d:\d\d)$/.test(text) ? text : text + "Z");
}

function fmtDay(value) {
    return parseDate(value).toLocaleDateString("ru-RU", {
        weekday: "short",
        day: "numeric",
        month: "long"
    });
}

function fmtTime(value) {
    return parseDate(value).toLocaleTimeString("ru-RU", {
        hour: "2-digit",
        minute: "2-digit"
    });
}

function fmtDateTime(value) {
    return parseDate(value).toLocaleString("ru-RU", {
        day: "numeric",
        month: "long",
        year: "numeric",
        hour: "2-digit",
        minute: "2-digit"
    });
}

function stars(count) {
    return '<span class="stars" aria-label="Оценка ' + count + ' из 5">' +
        "★".repeat(count) +
        '<span class="stars-off">' + "★".repeat(5 - count) + "</span></span>";
}

function imgTag(url, alt, className) {
    const src = safeUrl(url);
    if (!src) {
        return "";
    }
    if (/\.(mp4|webm)$/i.test(src)) {
        return '<video src="' + esc(src) + '" class="' + (className || "") +
            '" autoplay muted loop playsinline></video>';
    }
    return '<img src="' + esc(src) + '" alt="' + esc(alt) + '" class="' + (className || "") +
        '" loading="lazy" onerror="this.remove()">';
}

function toast(message, type) {
    const old = $(".toast");
    if (old) {
        old.remove();
    }
    const box = document.createElement("div");
    box.className = "toast " + (type || "");
    box.setAttribute("role", "status");
    box.textContent = message;
    document.body.appendChild(box);
    setTimeout(function () {
        box.remove();
    }, 4000);
}

function setMsg(element, text, type) {
    element.textContent = text;
    element.className = "form-msg " + (type || "");
}


function getToken() {
    const token = localStorage.getItem("vs_token");
    if (!token) {
        return null;
    }
    try {
        const payload = JSON.parse(atob(token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/")));
        if (payload.exp * 1000 < Date.now()) {
            clearSession();
            return null;
        }
    } catch (e) {
        clearSession();
        return null;
    }
    return token;
}

function getUser() {
    if (!getToken()) {
        return null;
    }
    try {
        return JSON.parse(localStorage.getItem("vs_user"));
    } catch (e) {
        return null;
    }
}

function saveUser(patch) {
    const user = Object.assign(getUser() || {}, patch);
    localStorage.setItem("vs_user", JSON.stringify(user));
}

function clearSession() {
    localStorage.removeItem("vs_token");
    localStorage.removeItem("vs_user");
}

function hasRole(role) {
    const user = getUser();
    return !!user && (user.roles || []).indexOf(role) !== -1;
}

function isAdmin() {
    return hasRole("Admin");
}

function isStaff() {
    return hasRole("Admin") || hasRole("Manager");
}

function logout() {
    clearSession();
    location.href = "/";
}

function errorText(data) {
    if (!data) {
        return "";
    }
    if (typeof data === "string") {
        return data;
    }
    if (Array.isArray(data)) {
        return data.map(function (item) {
            return IDENTITY_ERRORS[item.code] || item.description || item.message || "";
        }).join(" ");
    }
    if (data.message) {
        return data.message;
    }
    if (data.errors) {
        return Object.values(data.errors).flat().join(" ");
    }
    return data.title || "";
}


async function api(path, options) {
    options = options || {};
    const headers = {};
    const token = getToken();

    if (options.body !== undefined) {
        headers["Content-Type"] = "application/json";
    }
    if (token) {
        headers["Authorization"] = "Bearer " + token;
    }

    let response;
    try {
        response = await fetch(API_URL + path, {
            method: options.method || "GET",
            headers: headers,
            body: options.body !== undefined ? JSON.stringify(options.body) : undefined
        });
    } catch (e) {
        throw new Error("Нет связи с сервером. Проверьте, что бэкенд запущен.");
    }

    const text = await response.text();
    let data = text;
    try {
        data = text ? JSON.parse(text) : null;
    } catch (e) {
        data = text;
    }

    if (!response.ok) {
        if (response.status === 401 && token) {
            clearSession();
            throw new Error("Сессия истекла. Войдите снова.");
        }
        if (response.status === 403) {
            throw new Error("Недостаточно прав для этого действия.");
        }
        throw new Error(errorText(data) || "Ошибка сервера (" + response.status + ")");
    }
    return data;
}

function showError(container, error) {
    container.innerHTML = '<div class="empty">' + esc(error.message) + "</div>";
}


function initHeader() {
    const burger = $(".burger");
    const nav = $(".nav");
    if (burger && nav) {
        burger.addEventListener("click", function () {
            const open = nav.classList.toggle("open");
            burger.setAttribute("aria-expanded", open);
        });
    }

    const adminLink = $("#admin-link");
    if (adminLink && isStaff()) {
        adminLink.style.display = "";
    }
}

initHeader();
