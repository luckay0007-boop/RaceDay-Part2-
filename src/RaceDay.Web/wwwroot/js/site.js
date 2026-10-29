// RaceDay Web UI progressive enhancements
document.addEventListener("DOMContentLoaded", function () {
    // Auto-submit event selector forms when dropdown changes
    const autoSubmitSelects = document.querySelectorAll("[data-rd-autosubmit='true']");
    autoSubmitSelects.forEach(function (selectEl) {
        selectEl.addEventListener("change", function () {
            if (selectEl.form) {
                selectEl.form.submit();
            }
        });
    });

    // Populate Bootstrap delete confirmation modal safely using textContent and form action
    const deleteModal = document.getElementById("rdConfirmDeleteModal");
    if (deleteModal) {
        deleteModal.addEventListener("show.bs.modal", function (event) {
            const triggerBtn = event.relatedTarget;
            if (!triggerBtn) return;

            const itemName = triggerBtn.getAttribute("data-rd-item-name") || "this item";
            const formAction = triggerBtn.getAttribute("data-rd-form-action") || "";

            const nameSpan = deleteModal.querySelector("[data-rd-modal-item-name]");
            if (nameSpan) {
                nameSpan.textContent = itemName;
            }

            const confirmForm = deleteModal.querySelector("form");
            if (confirmForm && formAction) {
                confirmForm.setAttribute("action", formAction);
            }
        });
    }
});

