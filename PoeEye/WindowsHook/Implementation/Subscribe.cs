// This code is distributed under MIT license. 
// Copyright (c) 2015 George Mamaladze
// See license.txt or https://mit-license.org/

using WindowsHook.WinApi;

namespace WindowsHook.Implementation;

internal delegate HookResult Subscribe(WinHookCallback callbck);