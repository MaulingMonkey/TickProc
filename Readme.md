# TickProc

Simple service process runner / monitor.

**NOTE WELL:** This project is not yet considered "stable".  I will break backwards compatability:
* Config paths
* Config syntax

Features are unlikely to be removed however.

License: [Apache 2.0](LICENSE.txt)

# Example Config

This file can be found at `%APPDATA%\TickProc\TickProc.config.txt`

`Edit Config File` in the Notification Icon menu will also open this file.

```
# A realistic example:
run StaticWebServer.exe   http://localhost/test/foo/       C:\home\website\localhost\test\foo\
run StaticWebServer.exe   http://localhost/test/bar/       C:\home\website\localhost\test\bar\
run KeyValueWebServer.exe http://localhost/api/1/keyvalue/

# Duplicates will cause multiple instances to be launched:
run ping -t localhost
run ping -t localhost

# GUI apps also work fine:
run notepad
run notepad
```

* TickProc will launch 7 processes when started with the above config.
  * Console programs will have their window hidden and output redirected.
  * Redirected output may be collected for logging / error reporting.

* TickProc will close the processes when closed.
  * First, TickProc will attempt to close the process via Ctrl+C Ctrl+C Ctrl+Break (with short timeouts).  Well behaved command line programs should probably be closed by this.
  * Then, TickProc will try closing the main window (with another short timeout.)  Save dialogs and the like might keep the process open, but well behaved GUI programs should otherwise be closed.
  * Finally, TickProc will Kill the process.  It had it's chance!

* TickProc will monitor the config file.
  * Commenting out a line will cause the relevant process to be stopped per the same procedure as closing.
  * Adding new lines will cause the relevant processes to be started.
  * The processes corresponding to unmodified lines will be left alone.
