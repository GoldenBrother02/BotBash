const UrlParams = new URLSearchParams(window.location.search);
const Room = UrlParams.get("Room");
const Modal = document.getElementById("GameOverModal");
const Msg = Modal.querySelector("#GameOverMessage");
const Connection = new signalR.HubConnectionBuilder()
    .withUrl("/gamehub")
    .configureLogging(signalR.LogLevel.Information)
    .build();

let CurrentRoom = Room;



Connection.on("Connected", msg => console.log("Connected:", msg));

Connection.on("WorldUpdated", world => {
    RenderWorld(world);
});

Connection.on("GameEnded", (result) => {
    console.log("Game ended:", result);

    if (Modal) {
        Msg.textContent = result.includes("Victory") ? result : "It's a draw!";
        Modal.style.display = "flex";
    }
});

Connection.start().then(async () => {
    await Connection.invoke("JoinRoom", CurrentRoom).catch(err => console.error("JoinRoom failed:", err));
    await Connection.invoke("StartManualGame", CurrentRoom).catch(err => console.error(err));
});



document.querySelectorAll("#Controls button[data-action]").forEach(btn => {
    btn.addEventListener("click", async () => {
        const Action = btn.dataset.action;

        await Connection.invoke("SendManualBotAction", CurrentRoom, Action).catch(err => console.error("Manual Action failed:", err));
        await Connection.invoke("Tick", CurrentRoom).catch(err => console.error("Tick failed:", err));
    });
});

document.getElementById("LeaveBtn").addEventListener("click", async () => {
    await Connection.invoke("LeaveRoom", CurrentRoom).catch(err => console.error("LeaveRoom failed:", err));
    window.location.href = "/"; //Go back to home page
});

document.getElementById("HomeBtn").addEventListener("click", async () => {
    Modal.style.display = "none";

    await Connection.invoke("LeaveRoom", CurrentRoom).catch(err => console.error("LeaveRoom failed:", err));
    window.location.href = "/"; //Go back to home page
});

document.getElementById("RestartBtn").addEventListener("click", async () => {
    Modal.style.display = "none";

    await Connection.invoke("StartManualGame", CurrentRoom).catch(err => console.error(err));
});



function RenderWorld(world) {
    const World = document.getElementById("world");

    //Set grid size
    World.style.setProperty("--grid-width", world.width);
    World.style.setProperty("--grid-height", world.height);

    //Clear previous cells
    World.innerHTML = "";

    //Create a map
    const CellMap = {};
    for (const cell of world.cells) {
        CellMap[`${cell.x},${cell.y}`] = cell;
    }

    for (let y = 1; y <= world.height; y++) {
        for (let x = 1; x <= world.width; x++) {
            const CellDiv = document.createElement("div");
            CellDiv.classList.add("cell");

            const cell = CellMap[`${x},${y}`];

            if (!cell) {
                CellDiv.classList.add("empty");
            } else if (cell.hasPlayer) {
                CellDiv.classList.add("player");
                CellDiv.textContent = "P";
            } else {
                switch (cell.constructType) {
                    case "Wall": CellDiv.classList.add("wall"); CellDiv.textContent = "#"; break;
                    case "Spike": CellDiv.classList.add("spike"); CellDiv.textContent = "S"; break;
                    case "Danger": CellDiv.classList.add("danger"); CellDiv.textContent = "D"; break;
                    default: CellDiv.classList.add("empty"); break;
                }
            }
            World.appendChild(CellDiv);
        }
    }
}