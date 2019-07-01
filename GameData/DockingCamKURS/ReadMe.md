This mod adds a camera to all docking ports using a ModuleManager patch, as well as a part which can act as a mobile camera.

It looks ahead by the vertical axis of the vessel's and displays a minimum of necessary info for the docking maneuver 
- namely, range, speed, angle, alignment and when the correct trajectory occurred 
- time prior to docking with the mark that such trajectory will lead our vessel to dock without additional actions (small lamp will become green and will show time to docking). 
The camera window has three size presets. 

It also has three viewing modes:
	Color
	Black and white
	Infrared

Available options/abilities include:
	A powered zoom function, using a slider at the top of the window.
	A button to remove flight data from the screen.
	A button to add television interference 

The range of the camera parameters of night vision (RSMA) and presence of noise can be configured through CFG. 
The camera requires a target to operate.

The included external camera can show videostream only, has three modes of vision, and is able to rotate on two axes. 
For cameras installed on the side or under the belly of the aircraft (which gets inverted 
upside down) there is the mode of rotation of the image.  The camera can now be rotated in 90 degree increments.

In the CFG of the partcamera some meshes can be configured for use with other camera models.

All cameras have a title with its number
Multiple cameras can be working at the same time. 
Docking camera reports its target on its window title.

There is an experiment on the part camera:
A try to reproduce a surveillance (spy) activity. You need to be at a distance less than 1000 m near any targetable thingie, catch it on camera's screen and targeted. Then press the "⦿" button. 
A ray will be shot. If all requirements have met and there aren’t any obstacles on the ray's path, you'll sget experiment results  If something was wrong, then you just spent one bullet.  Experiments is limited by bullets (4 yellow balls around cam, disappearing one by one each time). 

Camera has 3 presets of shaders:
	noisy TV 1960th style
	TV 1980th style
	Standard:
		color, 
		b/w
		nightvision
Available via button. 

Also, using the toolbar button you can find info about nearby cameras installed on other vessels within transmission range.  If a camera has been activated on another vessel then a new window from the distant camera will appear when distance from first one will become less than 2500 (or 10k (customizable)). 

There is a "look at me" mode by activating it on distant camera. 
"Targetcam"  and "follow me" modes on your active vessel (set camera position using scrollers).

The cameras can transmit the videostream to nearby vessels, but only at a distance of up to 2,500 or 10,000 meters, configurable via the toolbar window


For Modders

The docking camera can now be used as a general purpose camera for your parts.

cameraPostion is the location of the camera on (or in this case, in) the part (docking port)
cameraUp would be the vector denoting the frame up direction (top of the frame, y axis) relative to cameraPosition
cameraForward would be the vector denoting the camera's facing forward direction (out the front of the camera, z axis) relative to cameraPosition.

Here is a complete config:

				MODULE
				{
					name = DockingCameraModule

					cameraName = grappleNode			// name of the transform where the camera is to be located
					cameraLabel = Grapple Camera		// Name of the camera in the PAW
					windowLabel = "Grapple View"		// label of the window
					
					slidingOptionWindow = false			// If false, sliding window doesn't appear, default is true
					crossDPAIonAtStartup = false		// specifies if the DPAI cross is on, default is true
					crossOLDDonAtStartup = false		// specifies if the OLDD cross is on, default is true
					targetCrossStockOnAtStartup = false	// specifies if the target cross is on, default is true

					noise = false						// specifies if there is noise

					// The following are used to properly position the camera if it needs to be adjusted			
					cameraForward = 0, -1, 0
					cameraUp = 0, 0, 1
					cameraPosition = 0.0, 0.25, 0.0
				}

There is a special dev mode for modders and players to be able to determine the correct values for the three values above.  To enable the devMode, add the following to the module definition:

					devMode = true

When this is true, there will be a new button on the PAW called "Camera Adjuster".  Click that, and a window will open up with many buttons which you can use to adjust the position.
The Delta value is the amount of adjustment to be done when a button is clicked
You can also toggle the four options which modify the window so you can see:

		slidingOptionWindow 			// If false, sliding window doesn't appear
		crossDPAIonAtStartup			// specifies if the DPAI cross is on 
		crossOLDDonAtStartup			// specifies if the OLDD cross is on
		targetCrossStockOnAtStartup		// specifies if the target cross is on

Finally, there is a "Save" button which willboth display the complete config and save the settings to a file; the file it is saved to is shown above the config window

Please note that this mode is NOT optimized for speed, and you may have performance issues while it is active