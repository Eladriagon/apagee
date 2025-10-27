# Apagee

Apagee is a self-hosted, minimalistic, fediverse-first, solo blog engine.

## Apa-what?

*Apagee* is a portmanteau (specifically, an [intercalative blend](https://en.wikipedia.org/wiki/Portmanteau#Total_blends)) of *apogee* and *page*.

## Features

* Single-user
* Quick and easy setup
* Cross-platform (Windows or Linux)
* Reverse proxy support
* Full fediverse participation
* Admin web interface

## Instructions

### ⚠️ THIS IS PRE-ALPHA! ⚠️

You have been warned. 🙂 (It's not dangerous, just buggy.)

### Linux (Debian)

Run this to self-install:

> `wget --cache=off -qO- https://raw.githubusercontent.com/Eladriagon/apagee/main/install/install.sh | bash`

You should always review shell scripts before executing them. Here is a link to the script to review:

https://raw.githubusercontent.com/Eladriagon/apagee/main/install/install.sh

It performs the following tasks:

* Installs required dependencies via `apt-get update && apt-get install ...`
* Creates user `apagee` / group `apagee`
* Downloads the release marked "Latest" from this Github repo
* Extracts it to `/home/apagee/apagee`
* Runs `setcap` to allow binding to 80/443
* (Re-)registers `apagee.service` in `systemctl`
* Starts the service

### All other OSes

Not yet supported but you're welcome to try!

The releases contain precompiled binaries that should run on most Linux and Windows OSes.

## Todo List

* [x] JSON configuration file with environment variable overrides
* [x] SQLite database
* [x] Automatic admin login creation with UI for password change
* [x] Auto-generated Fediverse signing keys stored locally
* [x] Author articles in Markdown
* [ ] Receive boosts/favorites from Fediverse
* [x] Draft articles
* [x] View article list
* [x] Delete articles
* [x] Accept and track followers, and respond to follow requests
* [ ] Send Fediverse activities
