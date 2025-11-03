const UrlParams = new URLSearchParams(window.location.search);
const room = UrlParams.get("room");
let currentRoom = "";
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/gamehub")
    .configureLogging(signalR.LogLevel.Information)
    .build();



connection.on("Connected", (msg) => {
    console.log("[SignalR] Connected event:", msg);
});

connection.on("WorldUpdated", (world) => {
    console.log("[SignalR] WorldUpdated event received:", world);
    renderWorld(world);
});



if (room === "Manual") {
    connection.start().then(() => {
        currentRoom = "ManualRoom1"
        connection.invoke("JoinRoom", currentRoom);
        connection.invoke("StartManualGame", currentRoom).catch(err => console.error(err));
    })
    document.getElementById("tickBtn").style.display = "block";

}

if (room === "Auto") {
    connection.start().then(() => {

        currentRoom = "AutoRoom1"
        connection.invoke("JoinRoom", currentRoom);
        connection.invoke("StartGame", currentRoom).catch(err => console.error(err));
    })
    document.getElementById("tickBtn").style.display = "none";
}



document.getElementById("tickBtn").addEventListener("click", async () => {
    try {
        await connection.invoke("Tick", "ManualRoom1"); //only 1 manual room (for now)
    } catch (err) {
        console.error("Tick failed:", err);
    }
});

document.getElementById("leaveBtn").addEventListener("click", async () => {
    try {
        await connection.invoke("LeaveRoom", currentRoom);
        window.location.href = "/"; //Go back to home page
    } catch (err) {
        console.error("LeaveRoom failed:", err);
    }
});


function renderWorld(world) {
    const container = document.getElementById("world");

    //Set grid size
    container.style.setProperty("--grid-width", world.width);
    container.style.setProperty("--grid-height", world.height);

    //Clear previous cells
    container.innerHTML = "";

    //Create a map
    const cellMap = {};
    for (const cell of world.cells) {
        cellMap[`${cell.x},${cell.y}`] = cell;
    }

    for (let y = 1; y <= world.height; y++) {
        for (let x = 1; x <= world.width; x++) {
            const cellDiv = document.createElement("div");
            cellDiv.classList.add("cell");

            const cell = cellMap[`${x},${y}`];

            if (!cell) {
                cellDiv.classList.add("empty");
            } else if (cell.hasPlayer) {
                cellDiv.classList.add("player");
                cellDiv.textContent = "P";
            } else {
                switch (cell.constructType) {
                    case "Wall": cellDiv.classList.add("wall"); cellDiv.textContent = "#"; break;
                    case "Spike": cellDiv.classList.add("spike"); cellDiv.textContent = "S"; break;
                    case "Danger": cellDiv.classList.add("danger"); cellDiv.textContent = "D"; break;
                    default: cellDiv.classList.add("empty"); break;
                }
            }

            container.appendChild(cellDiv);
        }
    }
}