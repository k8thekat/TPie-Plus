"""Copyright (C) 2021-2024 Katelynn Cadwallader.

This file is part of TPie-Plus.

TPie-Plus is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 3, or (at your option)
any later version.

TPie-Plus is distributed in the hope that it will be useful, but WITHOUT
ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public
License for more details.

You should have received a copy of the GNU General Public License
along with TPie-Plus; see the file COPYING.  If not, write to the Free
Software Foundation, 51 Franklin Street - Fifth Floor, Boston, MA
02110-1301, USA.
"""

import datetime
import json
import pathlib
from typing import Any, TypedDict


class PluginMaster(TypedDict):
    Author: str
    Name: str
    Punchline: str
    Description: str
    Changelog: str
    IsHide: str
    InternalName: str
    AssemblyVersion: str
    TestingAssemblyVersion: str
    ApplicableVersion: str
    DalamudApiLevel: int
    DownloadCount: int
    LastUpdate: int
    DownloadLinkInstall: str
    DownloadLinkUpdate: str
    Tags: list[str]
    IconUrl: str


class ProjectJSON(TypedDict):
    Name: str
    Author: str
    Punchline: str
    Description: str
    RepoUrl: str
    Tags: list[str]
    Changelog: str
    IconUrl: str


VERSION: str = ""


project_dir: pathlib.Path = pathlib.Path(__file__).parent.joinpath("TPie-Plus")
repo_url = "https://github.com/k8thekat/TPie-Plus/"

json_file: pathlib.Path = project_dir.joinpath("pluginmaster.json")
if json_file.exists() is False:
    msg = "Unable to locate the file. %s"
    raise FileNotFoundError(msg, json_file.as_posix())

csproj_file: pathlib.Path = project_dir.joinpath("TPie-Plus.csproj")
if csproj_file.exists() is False:
    msg = "Unable to locate the file. %s"
    raise FileNotFoundError(msg, csproj_file.as_posix())


cl_file: pathlib.Path = pathlib.Path(__file__).parent.joinpath("CHANGELOG.md")
if cl_file.exists() is False:
    msg = "Unable to locate the file. %s"
    raise FileNotFoundError(msg, cl_file.as_posix())


def _parse_csproj() -> Any:
    tokens: dict[str, tuple[str, str]] = {
        "version": ("<Version>", "</Version>"),
        "assemblyVer": ("<AssemblyVersion>", "</AssemblyVersion>"),
        "fileVer": ("<FileVersion>", "</FileVersion>"),
        "infoVer": ("<InformationalVersion>", "</InformationalVersion"),
    }
    print(f"Opening TPie-Plus.csproj. Path: {csproj_file.as_posix()}")  # noqa: T201
    with csproj_file.open("r+") as file:
        global VERSION  # noqa: PLW0603
        data: str = file.read()
        flag = False
        for value in tokens.values():
            start_idx = data.find(value[0])
            end_idx = data.find(value[1])
            if start_idx != -1 and end_idx != -1:
                # start_idx = `<`Ver...> | end_idx = `<`/Ver...>
                temp = data[start_idx : end_idx + len(value[1])]
                # First time through iteration to change the Version
                if flag is False:
                    ver = data[start_idx + len(value[0]) : end_idx]
                    VERSION = input(f"Current Version - {ver}| Version bump: ")  # pyright: ignore[reportConstantRedefinition]
                    flag = True

                if not VERSION:
                    msg = "Version has not been set!"
                    raise ValueError(msg)

                data = data.replace(temp, value[0] + VERSION + value[1])

        print(f"Updated TPie-Plus.csproj Version: {VERSION}")  # noqa: T201
        file.seek(0)
        file.truncate()
        file.write(data)


def _parse_json() -> Any:
    print(f"Opening pluginmaster.json. Path: {json_file.as_posix()}")  # noqa: T201
    with json_file.open("r+") as file:
        data: list[PluginMaster] = json.loads(file.read())
        cur_v: str = data[0].get("AssemblyVersion", "0.0.0.0")  # noqa: S104

        last_update: int = int(datetime.datetime.now(tz=datetime.UTC).timestamp())

        if cur_v == VERSION:
            msg: str = f"Verions are the same. Current: {cur_v} | New: {VERSION}"
            raise ValueError(msg, cur_v, VERSION)

        data[0]["AssemblyVersion"] = VERSION
        data[0]["ApplicableVersion"] = VERSION
        data[0]["TestingAssemblyVersion"] = VERSION
        data[0]["LastUpdate"] = last_update
        data[0]["DownloadLinkInstall"] = repo_url + f"/releases/download/{VERSION}/latest.zip"
        data[0]["DownloadLinkUpdate"] = repo_url + f"/releases/download/{VERSION}/latest.zip"
        print(f"Updated pluginmaster.json Version: {VERSION}")  # noqa: T201
        file.seek(0)
        file.truncate()
        file.write(json.dumps(data, indent=4))


_parse_csproj()
_parse_json()
