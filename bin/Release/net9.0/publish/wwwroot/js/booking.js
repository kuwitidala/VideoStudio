function initBooking(form) {
    const state = { serviceId: null, scheduleId: null };
    const msg = $("[data-msg]", form);
    const summary = $("[data-summary-box]", form);

    const preset = $('input[name="service"]:checked', form);
    if (preset) {
        state.serviceId = Number(preset.value);
    }

    const loggedIn = !!getUser();
    $("[data-submit]", form).style.display = loggedIn ? "" : "none";
    $("[data-login]", form).style.display = loggedIn ? "none" : "";

    function drawSummary() {
        const parts = [];
        const radio = $('input[name="service"]:checked', form);
        if (radio) {
            parts.push(radio.closest(".choice").dataset.summary);
        }
        const slot = $("[data-slot].on", form);
        if (slot) {
            parts.push(slot.dataset.summary);
        }
        summary.innerHTML = parts.length
            ? "<strong>Ваша запись:</strong> " + parts.map(esc).join(", ")
            : "Выберите услугу, дату и время.";
    }

    function selectDay(button) {
        $$("[data-day]", form).forEach(function (b) {
            b.classList.toggle("on", b === button);
        });
        $$("[data-times]", form).forEach(function (box) {
            box.style.display = box.dataset.times === button.dataset.day ? "" : "none";
        });
        $$("[data-slot]", form).forEach(function (b) {
            b.classList.remove("on");
        });
        state.scheduleId = null;
        drawSummary();
    }

    function selectSlot(button) {
        $$("[data-slot]", form).forEach(function (b) {
            b.classList.toggle("on", b === button);
        });
        state.scheduleId = Number(button.dataset.slot);
        drawSummary();
    }

    form.addEventListener("change", function (event) {
        if (event.target.name === "service") {
            state.serviceId = Number(event.target.value);
            drawSummary();
        }
    });

    form.addEventListener("click", function (event) {
        const dayButton = event.target.closest("[data-day]");
        const slotButton = event.target.closest("[data-slot]");
        if (dayButton) {
            selectDay(dayButton);
        }
        if (slotButton) {
            selectSlot(slotButton);
        }
    });

    form.addEventListener("submit", async function (event) {
        event.preventDefault();
        setMsg(msg, "");

        if (!state.serviceId || !state.scheduleId) {
            setMsg(msg, "Выберите услугу, дату и время.", "error");
            return;
        }

        const options = $$(".options input:checked", form).map(function (input) {
            return input.value;
        });
        const text = $("#booking-comment", form).value.trim();
        let comment = "";
        if (options.length) {
            comment += "Опции: " + options.join(", ") + ". ";
        }
        comment += text;

        const button = $("button[type=submit]", form);
        button.disabled = true;
        try {
            await api("/api/bookings", {
                method: "POST",
                body: { serviceId: state.serviceId, scheduleId: state.scheduleId, comment: comment.trim() || null }
            });
            form.innerHTML =
                '<div class="step"><h3>Заявка отправлена</h3>' +
                "<p>Мы свяжемся с вами и подтвердим запись. Статус можно отслеживать в личном кабинете.</p>" +
                '<a class="btn btn-primary" href="/account">Перейти в личный кабинет</a></div>';
        } catch (error) {
            setMsg(msg, error.message + " Обновите страницу, чтобы увидеть актуальное расписание.", "error");
            button.disabled = false;
        }
    });
}

const bookingForm = $("form.booking");
if (bookingForm) {
    initBooking(bookingForm);
}
