
document.addEventListener("DOMContentLoaded", () => {
    const board = document.querySelector(".kanban-board");
    const tokenForm = document.getElementById("antiforgery-form");
    const message = document.getElementById("board-message");

    if (!board || !tokenForm) return;

    let draggedCard = null;

    //start dragging a task
    board.addEventListener("dragstart", event => {
        const card = event.target.closest(".task-card");

        if (!card) return;

        draggedCard = card;
        card.classList.add("dragging");

        event.dataTransfer.effectAllowed = "move";
        event.dataTransfer.setData(
            "text/plain",
            card.dataset.taskId
        );
    });

    //finish the drag operation
    board.addEventListener("dragend", () => {
        if (draggedCard) {
            draggedCard.classList.remove("dragging");
        }

        draggedCard = null;

        board.querySelectorAll(".drag-over").forEach(zone => {
            zone.classList.remove("drag-over");
        });
    });

    //highlight valid drop zones
    board.addEventListener("dragover", event => {
        const zone = event.target.closest(".kanban-dropzone");

        if (!zone || !draggedCard) return;

        event.preventDefault();
        event.dataTransfer.dropEffect = "move";

        board.querySelectorAll(".drag-over").forEach(item => {
            if (item !== zone) {
                item.classList.remove("drag-over");
            }
        });

        zone.classList.add("drag-over");
    });

    board.addEventListener("dragleave", event => {
        const zone = event.target.closest(".kanban-dropzone");

        if (zone && !zone.contains(event.relatedTarget)) {
            zone.classList.remove("drag-over");
        }
    });

    //save the new task status
    board.addEventListener("drop", async event => {
        const zone = event.target.closest(".kanban-dropzone");

        if (!zone || !draggedCard) return;

        event.preventDefault();
        zone.classList.remove("drag-over");

        const taskId = draggedCard.dataset.taskId;
        const newStatus = zone.dataset.status;
        const currentColumn = draggedCard.closest(
            ".kanban-dropzone"
        );

        //no update if dropped in the same column
        if (currentColumn?.dataset.status === newStatus) {
            return;
        }

        const token = tokenForm.querySelector(
            'input[name="__RequestVerificationToken"]'
        )?.value;

        if (!token) {
            showMessage("Security token missing. Refresh the page.");
            return;
        }

        const data = new URLSearchParams({
            taskId: taskId,
            status: newStatus,
            __RequestVerificationToken: token
        });

        try {
            const response = await fetch(tokenForm.action, {
                method: "POST",
                headers: {
                    "Content-Type":
                        "application/x-www-form-urlencoded;charset=UTF-8"
                },
                body: data.toString()
            });

            if (!response.ok) {
                let errorText =
                    "Unable to update the task. Refresh and try again.";

                try {
                    const result = await response.json();

                    if (result.message) {
                        errorText = result.message;
                    }
                } catch {
                    //use the default error message
                }

                showMessage(errorText);
                return;
            }

            //reload so column placement and counts match the database
            window.location.reload();
        } catch {
            showMessage(
                "A network error occurred. Refresh the board to check the task status."
            );
        }
    });

    function showMessage(text) {
        if (!message) return;

        message.textContent = text;
        message.classList.remove("d-none");
    }
});
