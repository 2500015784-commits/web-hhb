document.addEventListener("submit", function (event) {
    const message = event.target.dataset.confirm;
    if (message && !window.confirm(message)) event.preventDefault();
});

document.querySelectorAll("[data-print-invoice]").forEach(function (button) {
    button.addEventListener("click", function () { window.print(); });
});
