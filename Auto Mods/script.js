async function checkForUpdates() {
    try {
        const response = await fetch('update.json'); // Update this URL as needed
        const data = await response.json();
        const currentVersion = "1.0.0"; // Replace with the current version of your app

        if (data.latestVersion > currentVersion) {
            alert(`A new version (${data.latestVersion}) is available! Download it <a href="${data.downloadUrl}">here</a>.`);
        } else {
            alert("You are using the latest version.");
        }
    } catch (error) {
        console.error("Error checking for updates:", error);
        alert("Failed to check for updates. Please try again later.");
    }
}

// Add event listener for the check update button
document.getElementById("check-update").addEventListener("click", checkForUpdates);
