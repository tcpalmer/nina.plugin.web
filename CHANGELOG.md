# Web Session History Viewer

## 1.1.2.1 - 2022-07-25
* Plugin options are now saved per NINA profile
* Now supports running another instance of NINA in 'share' mode
* Web server will now automatically redirect root path requests
* NINA profile name now displayed in the Web client
* Dates are now ISO formatted
* Bug fix: issue with domain of Y axes in quality chart
* Bug fix: issue with viewing full-size images when image path contains special characters

## 1.1.0.1 - 2022-04-20
* Bug fix: fixed problems with loading of problematic session histories
* Bug fix: better evaluation and handling of AF trend lines
* Added dome events to timeline

## 1.1.0.0 - 2022-04-14
* Added timeline plot of NINA events including images saved, AF, MF, and more
* Hover over events to see details (image thumbnails, AF curve plots)
* Added plot of image quality metrics: stars, HFR, ADU stats, guiding stats, temperature

## 1.0.2.0 - 2022-02-02
* You can now click an image thumbnail to view a full-sized version.  The viewer supports zoom and drag to inspect.  Zoom in/out with the viewer icons or the mouse wheel; pinch to zoom works on touch-enabled devices.
* You can adjust settings that control the quality and stretch of the full-sized images
* Option to support non-light images
* Added a Help screen for the Web app
* Menus reworked and some tweaks to the responsive design

## 1.0.1.0 - 2022-01-22
* Web server will now accept connections using any domain, not just localhost and IP
* Fixed issue where Web server startup fail could crash NINA

## 1.0.0.1 - 2022-01-21
* Fixed installation issue

## 1.0.0.0 - 2022-01-21
* Initial release
