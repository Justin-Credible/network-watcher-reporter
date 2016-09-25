# network-watcher-reporter

This is a quick and dirty command line tool used to take output from the [Wireless Network Watcher](http://www.nirsoft.net/utils/wireless_network_watcher.html) (WNW) app and send push notifications when unidentified devices are found on the local network.

Pushes are sent using the [Pushover API](https://pushover.net/api). 

## Overview

WNW has an option to execute an arbitrary executable when a new device is found. Unfortunately, it fires this command too often for me. It seems to fire after a device has been offline for a while, and then comes back online. This was undesirable for me, as I specifically wanted to get pushes when a new device is detected, but not for ones I was already aware of.

To achieve this, I set the _User Text_ for each device entry in WNW with a description of the device. When new devices come online, this field is not set. This command line utility checks to see if the _User Text_ field is empty, and if it is, will then send a push notification.

## Usage

I've tested this with Wireless Network Watcher v1.97.

1. Build this project using Visual Studio 2015
2. Place the binary `network-watcher-reporter.exe` and `network-watcher-reporter.exe.config` at your preferred location
3. Edit the configuration file to set your preferred log file path as well as Pushover API token/user.
4. Open Wireless Network Watcher
5. Go to Options → Advanced Options
6. Set your network adapter and IP address range
7. Enable _Activate the beep/tray balloon alert only if the device is detected in the first time_
8. Set _Background scan interval_ to your preferred time
9. Enable _Execute the following command when a new device is detected_ with the following command line, adjusting the path as necessary:
 
```D:\network-watcher-reporter.exe "%device_name%" "%mac_addr%" "%user_text%" "%adapter_company%" "%ip_addr%"```
