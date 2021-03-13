# Motor Importer

Cli tool for importing the Danish SKAT Motor registry dumps into a mongodb server.

This project mainly serves as a playground for different technologies and trying to come up with my own solutions for challenges like processing 80+ GB xml files

## Usage

* Clone project
```bash
git clone https://github.com/henrikse55/motor-importer.git
```

* Navigate to Importer.Cli project directory
* You then have a choice of either building it or running it directly via `dotnet run --`

There currently is only one command available, see Import Arguments for usage

## Import Arguments

### Output Mode | --output (mode)
There's currently 3 output modes present.

|Mode|Description|Note|
|----|:-----------:|----|
|Console|Prints the raw XML data to the console|Default|
|Dump|Dumps the raw XML data||
|Mongo|Write XML data to a mongo server or cluster|

### Data Source | --data-source (path) | --source (path)
Full path to either a remote or local xml dump.

The Cli ignores if the local source is zip or xml, if zip is found it will simply read the file directly from the zip.

When downloading from remote the Cli assumes it's a zip and will use a heavily opinionated "zip opener" so it can process the xml file as it downloads.

Using local zip or xml file

`--source /path/to/zip-or-xml`

using with remote FTP server

`--source ftp://[ftp-server-address]/path/to/zip`

---

### Mongo | --mongo (address-of-mongo[,additional])
Address of one or more MongoDb instance.

This is a comma (,) separated list when specifying multiple instances.

Single instance
```bash
--mongo remote
```
multiple Instances
```bash
--mongo remote1,remote2,remote3
```
---
See `--auth` argument for mongo authentication

### Mongo Authentication | --auth (username:password)

Use this to pass mongo credentials to the cli using colon as a separator.

---
`--auth MyMongoAccount:SuperSecurePassword`


## Full usage examples

Simply dumping of local file

`./importer.cli import --source /some/path/to/data.xml --output dump`

Uploading a remote source to a mongo instance

`./importer.cli import --source ftp://[ftp-server-address]/path/to/zip --output mongo --mongo 127.0.0.1 --auth mongo:SuperSecure`

## Notes
* While it should compile, all code remains untested in Windows
* The remote FTP capability remains highly opinionated to my needs and mileage may wary if different endpoints are used
* I do not have any relation with SKAT nor the FTP server provided by SKAT and therefore refrain from referencing the FTP server