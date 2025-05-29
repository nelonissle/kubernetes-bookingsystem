#!/usr/bin/env bash

dotnet clean

find . -type d -name "bin" -exec rm -rf {} +
find . -type d -name "obj" -exec rm -rf {} +
find . -type d -name "data" -exec rm -rf {} +
find . -type d -name "logs" -exec rm -rf {} +
