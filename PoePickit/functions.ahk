isActive := false

LWin::
	if (isActive == "False")
		return
	Send, {Control down}
	While (GetKeyState("LWin", "P") == 1)
	{
		Send, {VK43}
		sleep, 30
	}
	Send, {Control up}
	Send, {LWin up}
return

