function openWork(work) {
    const video = safeUrl(work.videoUrl);
    const isFile = /\.(mp4|webm)$/i.test(video);
    const modal = document.createElement("div");
    modal.className = "modal";
    modal.innerHTML =
        '<div class="modal-box" role="dialog" aria-modal="true" aria-label="' + esc(work.title) + '">' +
        '<button class="modal-close" type="button" aria-label="Закрыть">×</button>' +
        (isFile ? '<video src="' + esc(video) + '" controls autoplay></video>' : imgTag(work.imageUrl, work.title)) +
        "<h2>" + esc(work.title) + "</h2>" +
        "<p>" + esc(work.description) + "</p>" +
        (video && !isFile ? '<a class="btn btn-primary" href="' + esc(video) + '" target="_blank" rel="noopener">Смотреть видео</a>' : "") +
        "</div>";

    function close() {
        modal.remove();
        document.removeEventListener("keydown", onKey);
    }

    function onKey(event) {
        if (event.key === "Escape") {
            close();
        }
    }

    modal.addEventListener("click", function (event) {
        if (event.target === modal || event.target.closest(".modal-close")) {
            close();
        }
    });
    document.addEventListener("keydown", onKey);
    document.body.appendChild(modal);
    $(".modal-close", modal).focus();
}

document.addEventListener("click", function (event) {
    const button = event.target.closest(".work");
    if (!button) {
        return;
    }
    openWork({
        title: button.dataset.title,
        description: button.dataset.description,
        imageUrl: button.dataset.image,
        videoUrl: button.dataset.video
    });
});
