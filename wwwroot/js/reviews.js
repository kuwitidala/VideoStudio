async function setupReviewForm() {
    const box = $("#review-form");

    function show(state) {
        $$("[data-state]", box).forEach(function (block) {
            block.style.display = block.dataset.state === state ? "" : "none";
        });
    }

    if (!getUser()) {
        show("guest");
        return;
    }

    let allowed = isStaff();
    if (!allowed) {
        try {
            const mine = await api("/api/bookings/my");
            allowed = mine.some(function (b) {
                return b.status === "Confirmed";
            });
        } catch (error) {
            allowed = false;
        }
    }
    if (!allowed) {
        show("denied");
        return;
    }

    show("form");

    const form = $("form", box);
    const msg = $(".form-msg", box);

    form.addEventListener("change", function () {
        const value = Number((form.querySelector("input:checked") || {}).value || 0);
        $$(".rating-input label", form).forEach(function (label, index) {
            label.classList.toggle("on", index < value);
        });
    });

    form.addEventListener("submit", async function (event) {
        event.preventDefault();
        const checked = form.querySelector("input:checked");
        const text = $("#review-text", form).value.trim();

        if (!checked || !text) {
            setMsg(msg, "Поставьте оценку и напишите пару слов.", "error");
            return;
        }
        try {
            await api("/api/reviews", {
                method: "POST",
                body: { rating: Number(checked.value), text: text }
            });
            form.reset();
            $$(".rating-input label", form).forEach(function (label) {
                label.classList.remove("on");
            });
            setMsg(msg, "Спасибо! Отзыв появится на сайте после проверки.", "ok");
        } catch (error) {
            setMsg(msg, error.message, "error");
        }
    });
}

setupReviewForm();
