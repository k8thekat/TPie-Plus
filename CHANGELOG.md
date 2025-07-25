## Version - 3.0.2.0 - [ed9cf5d](https://github.com/k8thekat/TPie-Plus/commit/ed9cf5d)
#### Overall
Merge branch 'development' of https://github.com/k8thekat/TPie into development
- Refactored the logic around Rings and Icon spacing and interaction with the mouse.
- Icons scale based upon mouse position relative to the Icon.
- Icons nearby move away from the nearest Icon to the mouse.
- Improved documentation in multiple locations.

## Version - 3.0.1.0 - [1545ad5](https://github.com/k8thekat/TPie-Plus/commit/1545ad5)
#### Version bump to `3.0.1.0`

#### 3.0.1.0
- Version bumped `csproj` and `pluginmaster`.
- Updated `.gitignore`.
- Updated `TODO.md`.
- Sorted `using` statements in multiple files.
- Added an additional command helper. `/tpp`.
- Changed keyboard focus to the item search input field when adding an item through the `Item Settings` window.
- Added logic to `Item Settings` to automatically populate the item selectable list with current inventory items when `In Inventory` is checked.
- Added a summary to `FocusIfNeeded()`.
- Fixed math value truncating when calculating degree offsets for Icons.

## Version - 3.0.0.1 - [e3ba241](https://github.com/k8thekat/TPie-Plus/commit/e3ba241)
#### TPie-Plus.csproj
- Version bump to `3.0.0.1`

#### Overall
Merge branch 'development' of https://github.com/k8thekat/TPie into development
Merge pull request #2 from k8thekat/development"
Development"
- !BREAKING UPDATE! Will not load old rings due to config changes.
- Rewrite of window drawing for `RingSettings`.
- Added new art, new center ring and arrow.
	- Better logic for arrow to follow your cursor.
- Updated TODO. Woohoo
Create changelog.yml"

## Version - 2.1.0.1 - [6d16887](https://github.com/k8thekat/TPie-Plus/commit/6d16887)

#### CHANGELOG.md

- Version info from `2.1.0.0` added.

#### TPie-Plus.csproj

- Version bump to `2.1.0.1`

#### Development Testing

- Added building functionality.
- Testing gitHub actions.

## Version - 2.1.0.0 - [548ce2a](548ce2a784d7d7898609aa41be922f9aa32d188d)

- Initial build.

## Version - 2.1.0.1 - [714f8e](714f8eadfce9525a440a68736a9b07b6b99d81be)

- Bug Fixes + Better Keybinds
- Further backend setting changes and naming conventions to match `TPie+`.
- Attempted fix for Dalamud Icon urls.
- Added support for additional keybinds ("CTRL", "ALT", "SHIFT") can now be used by themselves or together.
- Fixed `ImGui` errors related to Push and Pop.
