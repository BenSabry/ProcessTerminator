# Downloads
<b>*Latest release*</b> [v0.1.0-beta](https://github.com/BenSabry/ProcessTerminator/releases/tag/v0.1.0-beta)<br />
<b>*All releases*</b> [releases](https://github.com/BenSabry/ProcessTerminator/releases)

# ProcessTerminator
```
Usage: ProcessTerminator [OPTIONS] process_name

Description:
    This program terminates processes with configurable options for graceful handling. 
    It allows you to:
        - Wait for another process to exit before proceeding.
        - Specify a delay before sending a close request.
        - Forcefully terminate if the process doesn't exit within the wait time.

    Ideal for controlled process termination, ensuring system stability.

Options:
    -h, --help          Show this help message and exit.
    -v, --version       Display the program version and exit.
    -m, --monitor       Use to specify a process to wait for before proceeding.
    -i, --interval      Control how often the monitored process is checked.
    -d, --delay         Set a time buffer before sending a close request.
    -c, --command       Send a custom command before attempting to close the process.
    -w, --wait          Set a time buffer to allow the target process to close naturally.

Arguments:
    process_name        The name of the process to terminate (required)

Note:
    All time-related options (such as delay intervals) are specified in seconds.

Examples:
    ProcessTerminator whatsapp
        Attept to exit 'whatsapp' process immediately.

    ProcessTerminator -m chrome firefox
        Wait for the 'chrome' process to exit before attempting to exit 'firefox'.

    ProcessTerminator -d 10 spotify
        Delay for 10 seconds before attempting to exit 'spotify'.

    ProcessTerminator -c "custom_command" exif
        Send a custom command to 'exif' before attempting to close it.
```
