#!/bin/bash

source config

echo "Signing and compressing..."
chmod -R a+xr "$APP"
codesign --deep --force --verify --verbose --timestamp --options runtime --entitlements "$ENTITLEMENTS" --sign "$CERTIFICATE" "$APP"
ditto -c -k --sequesterRsrc --keepParent "$APP" "$APP.zip"

echo "Uploading compressed app to Apple for notarisation..."
TEAM_ID=$(xcrun iTMSTransporter -m provider -u "$APPLE_ID" -p "$APPLE_APP_SPECIFIC_PASSWORD" | awk 'END{print $NF}')
xcrun notarytool submit --wait --apple-id $APPLE_ID --password $APPLE_APP_SPECIFIC_PASSWORD --team-id=$TEAM_ID "$APP".zip
spctl -a -v "$APP"
