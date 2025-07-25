# ruff: noqa
import pathlib
import subprocess  # noqa: S404

version = ""


project_dir: pathlib.Path = pathlib.Path(__file__).parent.joinpath("TPie-Plus")
repo_url = "https://github.com/k8thekat/TPie-Plus/"

cl_file: pathlib.Path = pathlib.Path(__file__).parent.joinpath("CHANGELOG.md")
if cl_file.exists() is False:
    msg = "Unable to locate the file. %s"
    raise FileNotFoundError(msg, cl_file.as_posix())

csproj_file: pathlib.Path = project_dir.joinpath("TPie-Plus.csproj")
if csproj_file.exists() is False:
    msg = "Unable to locate the file. %s"
    raise FileNotFoundError(msg, csproj_file.as_posix())


# Grab version from CSproj file.
with csproj_file.open("r") as file:
    data = file.read()
    token = ("<Version>", "</Version>")
    start_idx: int = data.find(token[0])
    end_idx: int = data.find(token[1])
    if start_idx != -1 and end_idx != -1:
        version: str = data[start_idx + len(token[0]) : end_idx]


# Grab Version from `CHANGELOG.md`
with cl_file.open(encoding="utf-8") as changelog:
    changelog_data = changelog.read()
    split_data = changelog_data.split("\n")
    ver_data = split_data[0]  # Version - *.*.*** (commit hash)
    ver_data = ver_data.split(" ")
    last_commit: str = ver_data[-1][1:8]
    cl_ver: str = ver_data[-3]
    # changelog.close()

# Compare CHANGELOG.md and __init__.py Versions.
if not version or cl_ver == version:
    msg = "Version has not been updated `TPie-Plus.csproj`: %s == `CHANGELOG.md`: %s"
    raise ValueError(msg, version, cl_ver)

# Verify that the current branch is `development`.
output: bytes = subprocess.check_output(["git", "branch"])
branch: str = output.decode("utf-8").strip("*").strip().split("\n")[0]
if branch != "development":
    msg = "<%s> | Current branch is not `development`: %s"
    raise RuntimeError(msg, "Changelog Generator", changelog)

# Verify that there are new commits.
output = subprocess.check_output(["git", "log"])
new_commit = output.decode("utf-8").split("\n")[0][7:14]
if new_commit == last_commit:
    msg = "No new commits since last version: %s == %s"
    raise RuntimeError(msg, last_commit, new_commit)

# Format the git log data into a dictionary for Changelog.
output = subprocess.check_output(["git", "log", '--format="%B"', last_commit + "..HEAD"])
files: dict[str, list[str]] = {}
cur_data = output.decode("utf-8")
cur_data = cur_data.strip().strip('"')
cur_data = cur_data.split("\n")
file_name = None
for entry in cur_data:
    _entry = entry
    if len(entry) == 0 or len(entry) == 1:
        continue
    if entry.startswith("$"):
        # This should skip our auto generated changelog commit from Github Actions.
        continue

    if entry.startswith('"'):
        _entry = entry.strip('"')

    elif entry.startswith("#"):
        file_name = entry[1:].strip()
        if file_name not in files:
            files[file_name] = []

    else:
        if entry.startswith("--"):
            _entry = "\t-" + entry[2:]
        if file_name is None:
            file_name = "Overall"
            files[file_name] = []
        files[file_name].append(_entry)


# Format the data into the `CHANGELOG.md`
user = "k8thekat"
project: str = "TPie-Plus"
set_version = f"## Version - {version} - [{new_commit[:7]}](https://github.com/{user}/{project}/commit/{new_commit})\n"
# add_changelog: str = f"#### CHANGELOG.md\n- Version info from `{cl_ver}` added.\n\n"
add_init: str = f"#### TPie-Plus.csproj\n- Version bump to `{version}`\n\n"
data = set_version + add_init
for file_name, file_changes in files.items():
    data: str = data + "#### " + file_name + "\n" + "\n".join(file_changes) + "\n\n"

data = data + changelog_data
with cl_file.open("r+", encoding="utf-8") as changelog:
    changelog.seek(0)
    changelog.truncate()
    changelog.write(data)
    # changelog.close()

# Github Actions Checkout/Commit
# id | k8thekat = 68672235
subprocess.run(["git", "config", "user.name", "github-actions[bot]"], check=False)
subprocess.run(["git", "config", "user.email", "68672235+github-actions[bot]@users.noreply.github.com"], check=False)
subprocess.run(["git", "add", "."], check=False)
subprocess.run(["git", "commit", "-m", f"$ Autogenerated Changelog for {version}"], check=False)
subprocess.run(["git", "push", "--force"], check=False)
