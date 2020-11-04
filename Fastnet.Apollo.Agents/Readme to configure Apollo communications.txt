
Standard configuration is shown in the following json fragment:

"MessengerOptions": {
        "MulticastIPAddress": "224.100.0.1", // 224.100.0.1 for the 'production' system, 224.100.0.2 for the 'development system
        "LocalCIDR": "192.168.0.0/24" //"10.0.0.0/24"
    },

1. "LocalCIDR" defines the subnet: "192.168.0.0/24" normally but also "10.0.0.0/24" in alka
2. "MulticastIPAddress" defines the multicat group for the music server and the agents (players) to communicate.
    a. "224.100.0.1" used when small-box is running the music server and music-box is running the agent (player)
    b. "224.100.0.2" used when asus is running the music server and asus is running the agent (player)
    c. "224.100.0.64" used when the music server is in Visual Studio on small-box and the agent is also running in Visual Studio on small-box
    d. "224.100.0.65" used when the music server is in Visual Studio on asus and the agent is also running in Visual Studio on asus