const loadingMessages = [
    "Connecting SQL Server...",
    "Loading Products...",
    "Loading Customers...",
    "Initializing Dashboard...",
    "Authenticating User...",
    "Preparing Workspace..."
];

let index = 0;

const loadingText = document.getElementById("loadingText");

let timer = null;

if (loadingText) {

    timer = setInterval(() => {

        loadingText.innerHTML = loadingMessages[index];

        index++;

        if (index >= loadingMessages.length) {
            index = loadingMessages.length - 1;
        }

    }, 600);

}


window.addEventListener("load", () => {

    setTimeout(() => {

        if (timer) {
            clearInterval(timer);
        }

        const loader = document.getElementById("preloader");

        if (loader) {

            loader.style.opacity = "0";

            setTimeout(() => {

                loader.style.display = "none";

            }, 1000);

        }

    }, 3500);

});