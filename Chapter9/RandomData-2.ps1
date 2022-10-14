$fileName = "appointments.csv"
"" | out-file $filename
$numLines = Get-Random -Minimum 2 -Maximum 20
$numFields = 5
1..$numLines |% {
	$line = ""
	1..$numFields |% {
		$len = Get-Random -Minimum 0 -Maximum 64
		$field = Get-RandomLine $len
		$line += ($field + ",")
	}
	$line.TrimEnd(",") | out-file -Append $fileName
}
