To maximize capabilities, it's recommended that you allow My Tokens to patch DotNetNuke core.

# How does it work?
DotNetNuke doesn't have a mechanism to provide other token replacers, so My Tokens only works with modules that have been already integrated with (see My Tokens page for list of supported standard and commercial modules). Patching the core means replacing DotNetNuke.dll with another file we precompile to fully support My Tokens.

# Benefits of patching the DNN core
Patching the core will make My Tokens available in all places where standard token replacing is supported. This includes a large number of standard or commercial modules.

#How to undo the patch
Note that My Tokens will make a backup of your current /bin/DotNetNuke.dll file and put it under /DesktopModules/avt.MyTokens/Backup. So if you later need to undo the patch, just copy the backup back to the /bin folder.

#How to apply the patch manually
Browse the public repository and download source code and patched DLLs for various versions of DotNetNuke. Contributions are also welcomed. 

#Contributions are welcomed
There are lots of DNN versions out there and it's a great effort to patch every one of them. We added notifications so when you request to patch a version we don't already have it here an email is sent to us. But if you can do the patch yourself and post it here (fork the pull request) that would be great. Note that we welcome all kind of patches, for example to add token replacement capabilities to meta tags, standard modules and so on.


Only Super User Accounts can use this function. Login with a Super User account or notify one...
