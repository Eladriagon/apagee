<div align="center">
<h1>Apagee</h1>
</div>

![GitHub Release](https://img.shields.io/github/v/release/eladriagon/apagee?style=for-the-badge)
 ![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/eladriagon/apagee/dotnet.yml?event=release&style=for-the-badge) ![GitHub Repo stars](https://img.shields.io/github/stars/eladriagon/apagee?style=for-the-badge&logo=github&label=%E2%AD%90%20stars&color=%23e8bf36)
 ![Mastodon Follow](https://img.shields.io/mastodon/follow/115376968747924209?domain=thepride.network&style=for-the-badge&logo=mastodon)



Apagee is a self-hosted, minimalistic, fediverse-first, solo blog engine.

## Apa-what?

*Apagee* is a portmanteau (specifically, an [intercalative blend](https://en.wikipedia.org/wiki/Portmanteau#Total_blends)) of *apogee* and *page*.

## Features

* Single-user
* Quick and easy setup
* Cross-platform (Windows or Linux)
* Reverse proxy support
* Full fediverse participation
  * Receive follows and (optionally) auto-follow back
  * Deliver new articles published in Apagee as notes to fediverse followers (appears on timelines as a "status")
  * Track count of boosts/shares and likes/favorites
  * #hashtag support
* Admin web interface

## Instructions

> [!WARNING]
> This is still in alpha. You're welcome to install it and try it out, but things may change while Apagee is still in active development!

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
* [x] Draft articles
* [x] View article list
* [x] Delete articles
* [x] Accept and track followers, and respond to follow requests
* [x] Custom profile properties
* [x] Send Fediverse activities
* [x] Receive boosts/favorites from Fediverse
* [x] Tags
* [x] Media (paste from clipboard)
* [ ] Better emoji support (Noto Color Emoji or Twemoji?)
* [ ] Dockerize and publish image
* [ ] "Primary" image for Fediverse posts
* [ ] Read more cutoff tag
* [ ] Project logo
* [ ] Project documentation
