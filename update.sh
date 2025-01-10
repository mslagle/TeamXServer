#!/bin/bash

set -e  # Exit immediately if any command fails
set -o pipefail  # Exit on pipeline errors

echo "Pulling the latest changes from Git..."
git pull

echo "Switching to the build directory..."
cd bin/Debug/net6.0

echo "Backing up TeamXServer directory..."
mv TeamXServer tempBackup

echo "Switching back to project root for rebuilding..."
cd ../../..

echo "Building the project..."
dotnet build

echo "Returning to the working directory..."
cd bin/Debug/net6.0

echo "Removing new userdata folder..."
rm -rf TeamXServer/userdata

echo "Restoring userdata from the backup..."
mv tempBackup/userdata TeamXServer/

echo "Cleaning up tempBackup directory..."
rm -rf tempBackup

echo "Script completed successfully!"
