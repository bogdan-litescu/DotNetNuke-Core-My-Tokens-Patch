To maximize capabilities, it's recommended that you allow My Tokens to patch DotNetNuke core.

# How does it work?
DotNetNuke doesn't have a mechanism to provide other token replacers, so My Tokens only works with modules that have been already integrated with (see My Tokens page for list of supported standard and commercial modules). Patching the core means replacing DotNetNuke.dll with another file we precompile to fully support My Tokens.

# Benefits of patching the DNN core
Patching the core will make My Tokens available in all places where standard token replacing is supported. This includes a large number of standard or commercial modules.

#How to undo the patch
Note that My Tokens will make a backup of your current /bin/DotNetNuke.dll file and put it under /DesktopModules/avt.MyTokens/Backup. So if you later need to undo the patch, just copy the backup back to the /bin folder.

#How to apply the patch manually
Browse the public repository and download source code and patched DLLs for various versions of DotNetNuke. Contributions are also welcomed. 

Only Super User Accounts can use this function. Login with a Super User account or notify one...
