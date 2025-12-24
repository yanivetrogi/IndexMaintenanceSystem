# SQL Server Credentials Manager

A secure command-line tool for managing SQL Server credentials used by the Index Maintenance System.

## Overview

This utility allows you to securely store, manage, and retrieve SQL Server connection credentials. It uses Windows Data Protection API (DPAPI) to encrypt sensitive information, ensuring that database passwords are never stored in plain text.

## Features

- Securely add and store SQL Server credentials
- List stored server credentials without revealing passwords
- Remove credentials that are no longer needed
- Support for custom credential file locations

## Installation

The application is compiled as a self-contained, single-file executable for Windows x64 systems.

To build from source:

```
dotnet publish -c Release
```

## Usage

```
CredentialsManager.exe [--file <filename>] <command> [arguments]
```

### Commands

#### Add a credential

```
CredentialsManager.exe add <server> <username> <password>
```

This adds or updates credentials for the specified server.

#### Remove a credential

```
CredentialsManager.exe remove <server>
```

This removes credentials for the specified server.

#### List credentials

```
CredentialsManager.exe list
```

Lists all servers and usernames (passwords are not displayed).

### Options

- `--file <filename>`: Specify a custom credentials file (default: credentials.bin)

## Examples

Add a credential:
```
CredentialsManager.exe add SQLSERVER01 sa mySecurePassword123
```

Use a custom credentials file:
```
CredentialsManager.exe --file C:\secure\my_credentials.bin add SQLSERVER02 admin P@ssw0rd
```

List all stored credentials:
```
CredentialsManager.exe list --file C:\secure\my_credentials.bin
```

Remove a credential:
```
CredentialsManager.exe remove SQLSERVER01 --file C:\secure\my_credentials.bin
```

## Security

- Credentials are encrypted using Windows DPAPI
- The encrypted credentials file can only be decrypted on the same machine by any process running on it

## Integration

This tool is part of the SQL Server Index Maintenance System and provides secure credential storage for database connections used by the main application.