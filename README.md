# NINA Web Plugin

The NINA Web plugin embeds a lightweight Web server that will provide access to details associated with your acquisition sessions.  A custom Web app is included to present the data.  If NINA is running and the plugin is enabled, you can view details from the current ('live') session as well as past sessions from any device on your local network.

The actual URLs to access the Web app will be displayed above once the plugin is installed:
* Local Address: for a browser running on this computer
* Local Network Address: for a browser running on any device on your local network

The local network address will use the IP of the computer running NINA.  Assuming this IP/port are available on your local network, any device (e.g. phone or tablet) can connect.  If you configure your local network so that this IP is static, then this URL will stay constant.  Otherwise it will change if this computer is assigned a new IP.

## Web App Usage
* By default, the plugin will keep the most recent 10 days of session history.  If you start NINA and enable the Web Plugin, then any available past sessions can be viewed in the Web app.  You can change the number of days to keep in the options.
* A short time after your first image is saved in the current session, that new session will be appear in the 'Select Session' dropdown.  Select it and you can monitor as new images are saved (it will check for changes every 10 seconds).
* By default, the app will sort the images newest first so the latest image always appears at the top.  However, if you change the table sort order, the latest could appear anywhere depending on the sort column.
* If the session has multiple targets, they will be presented in separate sections under the session.  Click the target name to open/close the target details.
* Click Show Console to see app status messages.  If this button changes color, click to see what the problem is.  The most common issue is that the Web app is still running but the plugin was disabled or NINA was stopped.  In that case, just close the browser window/tab.
* If the Web app is in a weird state, just click reload in the browser.

[Web client source repositiory](https://github.com/tcpalmer/nina.plugin.web.client)

## Getting Help
Ask for help in the #plugin-discussions channel on the NINA project [Discord server](https://discord.com/invite/rWRbVbw).

