// This code is distributed under MIT license. 
// Copyright (c) 2015 George Mamaladze
// See license.txt or https://mit-license.org/

using WindowsHook.WinApi;

namespace WindowsHook.Implementation;

/// <summary>
///  Returns: True = continue processing, False = stop callback chain
/// </summary>
public delegate bool WinHookCallback(WinHookCallbackData data);