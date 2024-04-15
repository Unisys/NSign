@echo off

dotnet restore --force-evaluate NSign.sln
dotnet restore --force-evaluate examples/NSignExamples.sln