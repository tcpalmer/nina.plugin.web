using System.Reflection;
using System.Runtime.InteropServices;

[assembly: Guid("81B04674-EA65-4FE8-B79B-A77C1D209183")]

[assembly: AssemblyTitle("Web Session History Viewer")]
[assembly: AssemblyDescription("Embedded Web server and app providing acquisition session history details")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Tom Palmer @tcpalmer")]
[assembly: AssemblyProduct("Web Session History Viewer")]
[assembly: AssemblyCopyright("Copyright © 2022")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: AssemblyVersion("1.1.0.0")]
[assembly: AssemblyFileVersion("1.1.0.0")]

// The minimum Version of N.I.N.A. that this plugin is compatible with
[assembly: AssemblyMetadata("MinimumApplicationVersion", "2.0.0.2059")]

[assembly: AssemblyMetadata("License", "MPL-2.0")]
[assembly: AssemblyMetadata("LicenseURL", "https://www.mozilla.org/en-US/MPL/2.0/")]
[assembly: AssemblyMetadata("Repository", "https://github.com/tcpalmer/nina.plugin.web/")]
[assembly: AssemblyMetadata("FeaturedImageURL", "https://raw.githubusercontent.com/tcpalmer/nina.plugin.web/main/NINA.Plugin.Web/assets/web-plugin-icon.png?raw=true")]
[assembly: AssemblyMetadata("ScreenshotURL", "https://raw.githubusercontent.com/tcpalmer/nina.plugin.web/main/NINA.Plugin.Web/assets/screenshot1-1100.png?raw=true")]
[assembly: AssemblyMetadata("AltScreenshotURL", "https://raw.githubusercontent.com/tcpalmer/nina.plugin.web/main/NINA.Plugin.Web/assets/screenshot2-1100.png?raw=true")]

[assembly: AssemblyMetadata("LongDescription", @"The Web Session History Viewer embeds a lightweight Web server that will provide access to details associated with your acquisition sessions.  A custom Web app is included to present the data.  If NINA is running and the plugin is enabled, you can view details from the current ('live') session as well as past sessions from any device on your local network.

The actual URLs to access the Web app will be displayed above once the plugin is installed.  The IP Address URL will use the IP of the computer running NINA.  Assuming this IP/port are available on your local network, any device (e.g. phone or tablet) can connect.  See network notes below.

## Web App Usage
* By default, the plugin will keep the most recent 10 days of session history.  If you start NINA and enable the Web Plugin, then any available past sessions can be viewed in the Web app.  You can change the number of days to keep in the options.
* As soon as the Web plugin is enabled, you can begin viewing the new session in a browser (select it from the Sessions dropdown).  Initially, only the NINA Start event will appear in the event timeline.  However, the Web app will begin checking for updates (new events and saved images) and display them.
* The event timeline will show various events of interest (NINA start/stop, sequence start/stop, center/platesolve, autofocus, meridian flip, etc) as well as all saved images grouped by filter.  You can hover over events (or press on touch-enabled devices) to see details like event types/times, image acquisition info and thumbnail, and autofocus curves.
* As soon as any images are saved for a target, a plot of image quality metrics will be shown.  This is very similar to the NINA HFR History plot in the Imaging tab where you can select metrics to display on the left and right.  Hover/press on the data points to see details for that image.
* Both the event timeline and the quality plots support zooming into a region of interest.  Use the left and right travellers in the scroll bar below the chart to zoom.  Be aware however, that the zoom will reset to min/max if new data arrives and impacts the plot.
* As new images are saved, they will appear in the panel for the active target, in a table below the quality plot.  By default, the app will sort the images newest first so the latest image always appears at the top.  However, if you change the table sort order, the latest could appear anywhere depending on the sort column.
* Click an image thumbnail to see a full-size version of the image (assuming you haven't moved or deleted the original).  Use the mouse wheel to zoom in/out.  Pinch also works on touch-enabled devices.
* If the session has multiple targets, they will be presented in separate sections under the session.  Click the target name to open/close the target details.
* There will only be one session history file for each run of NINA.  Disabling and enabling the plugin itself won't start a new session. 
* Click the Console menu to see app status messages.  If this button changes color, click to see what the problem is.  The most common issue is that the Web app is still running but the plugin was disabled or NINA was stopped.  In that case, just close the browser window/tab.
* Click the Help menu to see help for the Web app.
* Click the Settings icon to adjust the settings that control the quality and stretch of the full-sized images.
* If the Web app is in a weird state, just click reload in the browser.
* Session history and Web server logs are stored in the WebPlugin directory under the main NINA directory.

## Network Notes

If you only want to use the Web app from a browser running directly on the computer running NINA, you're all set.  However, you can also use the Web app from any device on your local network.  To do so it's usually sufficient to configure NINA to communicate through your firewall:
* Open Settings and search for Windows Defender Firewall
* Click 'Allow an app or feature through Windows Defender Firewall'
* Ensure NINA is enabled, at least for your private network.

If you configure your local network so that this IP is static (typically on your router), then the IP address URL will stay constant.  Otherwise it will change if this computer is assigned a new IP.

In addition to the URLs shown above, you should be able to use any domain name that resolves to the IP of the computer running NINA (or is NATed to it).  For example, if the IP of this computer is 192.168.0.123, DNS for myhost.mynetwork resolves to that IP, and you're using port 80, then you should be able to put http://myhost.mynetwork/dist in a browser and it should work on your local network (provided that IP/port aren't blocked).  Use nslookup to check your name resolution.  If your desired domain name doesn't resolve properly, then it's not going to work in a URL.  An exception is the host name of a Windows computer.  In that case, nslookup won't resolve but you can use it in a URL at least on a Windows computer that can resolve the name.

If this doesn't work you'll need to search around and figure it out - anything else is beyond the scope of this.

## Getting Help
* Ask for help in the #plugin-discussions channel on the NINA project [Discord server](https://discord.com/invite/rWRbVbw).
* [Plugin source code](https://github.com/tcpalmer/nina.plugin.web)
* [Web app source code](https://github.com/tcpalmer/nina.plugin.web.client)
* [Change log](https://github.com/tcpalmer/nina.plugin.web/blob/main/CHANGELOG.md)

The Web Session History Viewer is provided 'as is' under the terms of the [Mozilla Public License 2.0](https://github.com/tcpalmer/nina.plugin.web/blob/main/LICENSE.txt)
")]

[assembly: ComVisible(false)]
