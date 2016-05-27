LWin::
	While (GetKeyState("LWin", "P"))
	{
		IfWinNotActive, Path of Exile
		{
		    break
		}

		Send, {Control down}
		Send, {VK43}
		Send, {Control up}
		sleep, 100
	}
	Send, {LWin up}
return
