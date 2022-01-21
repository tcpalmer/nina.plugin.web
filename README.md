# Web Session History Viewer

The Web Session History Viewer embeds a lightweight Web server that will provide access to details associated with your acquisition sessions.  A custom Web app is included to present the data.  If NINA is running and the plugin is enabled, you can view details from the current ('live') session as well as past sessions from any device on your local network.

The actual URLs to access the Web app will be displayed above once the plugin is installed:
* Local Address: for a browser running on this computer
* Local Network Address: for a browser running on any device on your local network

The local network address will use the IP of the computer running NINA.  Assuming this IP/port are available on your local network, any device (e.g. phone or tablet) can connect.  See network setup below.

## Web App Usage
* By default, the plugin will keep the most recent 10 days of session history.  If you start NINA and enable the Web Plugin, then any available past sessions can be viewed in the Web app.  You can change the number of days to keep in the options.
* A short time after your first image is saved in the current session, that new session will be appear in the 'Select Session' dropdown.  Select it and you can monitor as new images are saved (it will check for changes every 10 seconds).
* By default, the app will sort the images newest first so the latest image always appears at the top.  However, if you change the table sort order, the latest could appear anywhere depending on the sort column.
* If the session has multiple targets, they will be presented in separate sections under the session.  Click the target name to open/close the target details.
* There will only be one session history file for each run of NINA.  Disabling and enabling the plugin itself won't start a new session. 
* Click Show Console to see app status messages.  If this button changes color, click to see what the problem is.  The most common issue is that the Web app is still running but the plugin was disabled or NINA was stopped.  In that case, just close the browser window/tab.
* If the Web app is in a weird state, just click reload in the browser.
* Session history and Web server logs are stored in the WebPlugin directory under the main NINA directory.

## Network Setup

If you only want to use the Web app from a browser running directly on the computer running NINA, you're all set.  However, you can also use the Web app from any device on your local network.  To do so it's usually sufficient to configure NINA to communicate through your firewall:
* Open Settings and search for Windows Defender Firewall
* Click 'Allow an app or feature through Windows Defender Firewall'
* Ensure NINA is enabled, at least for your private network.

If you configure your local network so that this IP is static (typically on your router), then the IP and the Web app URL will stay constant.  Otherwise it will change if this computer is assigned a new IP.

If this doesn't work you'll need to search around and figure out why.

[Web client source repositiory](https://github.com/tcpalmer/nina.plugin.web.client)

## Getting Help
Ask for help in the #plugin-discussions channel on the NINA project [Discord server](https://discord.com/invite/rWRbVbw).

