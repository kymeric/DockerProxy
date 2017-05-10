# DockerProxy
Proxies loopback (localhost) traffic for Docker Windows Containers to work around Windows NAT limitations.

## Why
Windows Containers running on Docker for Windows don't accept connections from/to localhost.  There are several references to this issue:

[Scott Hanselman calling it a bug](https://www.hanselman.com/blog/ExploringASPNETCoreWithDockerInBothLinuxAndWindowsContainers.aspx)

[This GitHub issue (that was closed)](https://github.com/docker/for-win/issues/204)

Scott classified it as a bug but I haven't been able to find any further information explaining if a fix is being worked on, or when we could expect it to land.  If anyone has more details on the plans for resolving this or if it's going to be resolved, I'd like to hear that and I can add that here.  But in the meantime this issue has been enough of a challenge for me over the last 6 months that I decided to put this utility together as a workaround.

## What
This is a simple dotnet core console app that acts as a TCP proxy.  In local testing it seems to work pretty reliably for any TCP/HTTP/WebSocket traffic.  It uses the docker CLI to monitor running docker instances and which ports they're exposing.  It then listens on localhost:{port} for incoming connections, proxying them on to the correct container's NAT IP address.

![Screenshot](http://i.imgur.com/6nIqQY5.png)

It will detect new containers launching or existing containers exiting and listen/stop accordingly.  So far I've only tested this with simple/basic docker nat networking.  It may work for more advanced docker networking configurations but I haven't tested them yet.  Please file issues for any problems you run into, or submit a pull request if you have the time to help out.

## Future
As mentioned, I don't know the plans for resolving the NAT limitation in Windows.  I'd really like for that to be resolved so this utility wouldn't be necessary.  But if that isn't going to get resolved, this sort of functionality probably makes a lot more sense to fold into Docker for Windows.  Until then, I'll be happy to take pull requests, take on some additional contributors if there's any interest there, or work on cleaning up issues.
