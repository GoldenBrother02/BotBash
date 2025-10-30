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

connection.start()
    .then(() => {
        console.log("[SignalR] Connected to gamehub");
        // start match
        connection.invoke("StartGame").catch(err => console.error(err));
    })
    .catch(err => console.error("[SignalR] Connection error:", err));

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
