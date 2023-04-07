There is a problem with StaticWebAssets resolver - it expects that RCL has a non-empty "wwwroot", which is not always the case.
Having this file with "Copy if newer" enforces wwwroot to be created. 