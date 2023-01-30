#!/bin/bash
#$1 Solution File
#$2 Configuration
#$3 Platform
#$4 IPA Output Path
#$5 Sign Identity Certificate
#$6 Provisioning Profile UUID
#$7 Java SDK location

MSBUILD2019=/Applications/Visual\ Studio.app/Contents/Resources/lib/monodevelop/bin/MSBuild/Current/bin/MSBuild.dll
MSBUILD2022=/Applications/Visual\ Studio.app/Contents/MonoBundle/MSBuild/Current/bin/MSBuild.dll
MSBUILDPATH=

if test -f "$MSBUILD2019"; then
    MSBUILDPATH=$MSBUILD2019
else
    MSBUILDPATH=$MSBUILD2022
fi

mono "$MSBUILDPATH" /t:Clean,Build "$1" /p:Configuration="$2" /p:Platform="$3" /p:IpaPackageDir="$4" /p:Codesignkey=\"$5\" /p:CodesignProvision="$6" /p:BuildIpa=true /p:JavaSdkDirectory="$JAVA_HOME_11_X64"