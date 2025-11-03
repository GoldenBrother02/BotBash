const UrlParams = new URLSearchParams(window.location.search);
const room = UrlParams.get("room");
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
        connection.invoke("JoinRoom", "ManualRoom1");
        connection.invoke("StartManualGame", "ManualRoom1").catch(err => console.error(err));
    })
    document.getElementById("tickBtn").style.display = "block";

}

if (room === "Auto") {
    connection.start().then(() => {
        connection.invoke("JoinRoom", "AutoRoom1");
        connection.invoke("StartGame", "AutoRoom1").catch(err => console.error(err));
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
    console.log("[Render] Rendering world", world);

    const grid = Array.from({ length: world.height }, () =>
        Array(world.width).fill(".")
    );

    for (const cell of world.cells) {
        let symbol = ".";
        if (cell.constructType === "Wall") symbol = "#";
        else if (cell.constructType === "Spike") symbol = "S";
        else if (cell.constructType === "Danger") symbol = "D";
        else if (cell.hasPlayer) symbol = "P";

        if (grid[cell.y - 1] && grid[cell.y - 1][cell.x - 1])
            grid[cell.y - 1][cell.x - 1] = symbol;
    }

    document.getElementById("world").textContent =
        grid.map(row => row.join(" ")).join("\n");
}
