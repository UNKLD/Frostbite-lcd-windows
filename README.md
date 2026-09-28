# Frostbite-lcd-windows
    This program allows to control the Dragos frostbite lcd to display cpu temps "Without installing the proprietory software"

# Windows Task Scheduler Instructions to run on start up

Configure the Tabs:
General Tab:

Name: FrostBite LCD Monitor

Check Run with highest privileges (This allows the executable to access CPU temperature diodes without a UAC pop-up).

Configure for: Select Windows 10 / Windows 11.

Triggers Tab:

Click New...

Begin the task: Select At log on.

Under Advanced settings, check Delay task for: and set it to 5 seconds (Gives Windows time to enumerate the USB serial port COM8 on boot).

Click OK.

Actions Tab:

Click New...

Action: Start a program

Program/script:

If using the VBScript runner (Option A):

Program/script: wscript.exe

Add arguments (optional): "C:\Tools\FrostbiteMonitor\run_hidden.vbs"

If launching the .exe directly:

Program/script: "C:\Tools\FrostbiteMonitor\FrostbiteMonitor.exe"

Start in (optional): C:\Tools\FrostbiteMonitor\ (Crucial: set this to the folder containing your executable).

Click OK.

Conditions Tab:

Uncheck Start the task only if the computer is on AC power (if on a laptop/UPS).

Uncheck Stop if the computer switches to battery power.

Settings Tab:

Check Allow task to be run on demand.

Uncheck Stop the task if it runs longer than... (Crucial: otherwise Windows will kill the monitor after 3 days).

If the task is already running, then the following rule applies: Do not start a new instance.

Click OK to save the task.

Step 4: Test the Setup
In Task Scheduler, click Task Scheduler Library in the left sidebar.

Find FrostBite LCD Monitor in the list, right-click it, and select Run.

Check your cooler's LCD: within 5–10 seconds, the temperature readout will begin updating.

Check your screen: no black console window or terminal icon will appear.