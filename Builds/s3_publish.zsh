BUILD_ROOT="Release/Windows/IL2CPP/Player"

[ -d $BUILD_ROOT ] && aws s3 sync $BUILD_ROOT s3://swihoni-games/Windows \
--exclude '*BackUpThisFolder_ButDontShipItWithYourGame/*' \
--exclude '*.zsh' \
--exclude '*Config.vfc' \
--exclude '.git/*' \
--exclude '.gitignore' \
--delete
