#!/usr/bin/env python3
from datetime import datetime
import os
import argparse

'''
This creates a xml file to make net sparkle updater find updates to the app
see more info here https://github.com/NetSparkleUpdater/NetSparkle#app-cast

Make sure to run it from root directory so it can replace the correct appcast file
'''

parser = argparse.ArgumentParser()
parser.add_argument("version", nargs='?', default='6.0.0.0', help="The version to create the appcast with in the format x.x.x.x (default: 6.0.0.0). NOTE: auto-update is disabled in this build.")

args = parser.parse_args()

formatted_pubdate = datetime.utcnow().strftime("%a, %d %b %Y %H:%M:%S +0000")
version = args.version
if version.startswith("v"):
    version = version[1:]

file_contents = ''

if os.path.exists("appcast.xml"):
    with open("appcast.xml", 'r') as file:
        file_contents = file.read()
    os.remove("appcast.xml")

# the xml is copied from the sample in the readme
xml = f'''<?xml version="1.0" encoding="UTF-8"?>
<rss xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns:sparkle="http://www.andymatuschak.org/xml-namespaces/sparkle" version="2.0">
    <channel>
    <title>Needlelight Update (disabled)</title>
    <link>https://raw.githubusercontent.com/Aarav2709/Needlelight/master/appcast.xml</link>
        <language>en</language>
    </channel>
</rss>'''

item = f'''        <item>
            <title>Needlelight Update v{version}</title>
            <sparkle:releaseNotesLink>
                https://raw.githubusercontent.com/Aarav2709/Needlelight/static-resources/Changelogs/v{version}.md
            </sparkle:releaseNotesLink>
            <pubDate>{formatted_pubdate}</pubDate>
            <enclosure url="https://github.com/Aarav2709/Needlelight/releases/download/v{version}/Needlelight.AU.exe"
                sparkle:version="{version}"
                sparkle:os="windows"
                length="12288"
                type="application/octet-stream"/>
        </item>'''

replace_target = "<language>en</language>"

if file_contents == '':
    file_contents = xml

file_contents = file_contents.replace(replace_target, replace_target + "\n" + item)

with open("appcast.xml", "w") as f:
    f.write(file_contents)

