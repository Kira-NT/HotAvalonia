# HotAvalonia.Remote

[![GitHub Build Status](https://img.shields.io/github/actions/workflow/status/Kira-NT/HotAvalonia/build.yml?logo=github)](https://github.com/Kira-NT/HotAvalonia/actions/workflows/build.yml)
[![Version](https://img.shields.io/github/v/release/Kira-NT/HotAvalonia?sort=date&label=version)](https://github.com/Kira-NT/HotAvalonia/releases/latest)
[![License](https://img.shields.io/github/license/Kira-NT/HotAvalonia?cacheSeconds=36000)](https://github.com/Kira-NT/HotAvalonia/blob/HEAD/LICENSE.md)

`HotAvalonia.Remote` *(also known as `HotAvalonia Remote File System`, or just `HARFS` for short)* is a minimalistic, secure *(hopefully)*, read-only file system server compatible with HotAvalonia. It was designed with the sole purpose of enabling hot reload on remote devices.

----

## Build

If you want to compile the project as a standalone native executable, run:

```sh
dotnet publish --ucr -f net9.0 -p:AssemblyName=harfs -o ./dist
```

Alternatively, if you really care about the size of the resulting binary, you can also add an option that enables UPX compression:

```sh
dotnet publish --ucr -f net9.0 -p:AssemblyName=harfs -p:PublishLzmaCompressed=true -o ./dist
```

After that, retrieve your very own `harfs` *(or `harfs.exe`)* executable from the `./dist` directory.

----

## Usage

```
Usage: harfs [options]

Run a secure remote file system server compatible with HotAvalonia.

Examples:
  harfs --root "C:/Files" --secret text:MySecret --address 192.168.1.100 --port 8080
  harfs -r "/home/user/files" -s env:MY_SECRET -e 0.0.0.0:20158 --allow-shutdown-requests

Options:
  -h, --help
      Displays this help page.

  -v, --version
      Displays the application version.

  -r, --root <root>
      Specifies the root directory for the remote file system.
      The value must be a valid URI.
      Default: The current working directory.

  -s, --secret <secret>
      Specifies the secret used for authenticating connections.
      The secret can be provided in several formats:
        • text:<secret>
            Provide the secret as plain text (UTF-8 encoded).
        • text:utf8:<secret>
            Provide the secret as plain text (UTF-8 encoded).
        • text:base64:<base64secret>
            Provide the secret as a Base64-encoded string.
        • env:<env-var>
            Read the secret from the specified environment variable as plain text.
        • env:utf8:<env-var>
            Read the secret from the specified environment variable as plain text.
        • env:base64:<env-var>
            Read the secret from the specified environment variable as a Base64-encoded string.
        • file:<path>
            Read the secret from the file at the specified path.
        • stdin
            Read the secret from standard input as plain text.
        • stdin:utf8
            Read the secret from standard input as plain text.
        • stdin:base64
            Read the secret from standard input as a Base64-encoded string.

  -a, --address <address>
      Specifies the IP address on which the server listens.
      Default: All available network interfaces.

  -p, --port <port>
      Specifies the port number on which the server listens.
      The port must be a positive integer between 1 and 65535.
      Default: 20158.

  -e, --endpoint <endpoint>
      Specifies the complete endpoint (IP address and port) for the server in the format "IP:port".
      This option overrides the individual --address and --port settings.

  -c, --certificate <path>
      Specifies the path to the X.509 certificate file used for securing connections.
      If provided, the server will use the certificate to establish SSL/TLS communication.

  -d, --max-search-depth <depth>
      Specifies the maximum search depth for file searches.
      A positive value limits the number of file paths returned.
      A value of 0 or less indicates no limit.
      Default: 0.

  -t, --timeout <timeout>
      Specifies the timeout duration in milliseconds before the server shuts down
      if no clients have connected during the provided time frame.
      A positive value sets the timeout period.
      A value of 0 or less indicates no timeout.
      Default: 0.

  --allow-shutdown-requests
      When specified, allows the server to accept shutdown requests from clients.
```

----

## License

Licensed under the terms of the [MIT License](https://github.com/Kira-NT/HotAvalonia/blob/HEAD/LICENSE.md).
