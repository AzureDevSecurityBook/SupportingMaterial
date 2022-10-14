Set-StrictMode -Version latest

function Get-RandomLine {
param (
	[Parameter(Mandatory)] [int] $len
)

	$charSet = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789{]+-[*=@:)}$^%;(_!&amp;#?>/|.'.ToCharArray()
	$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
	$bytes = New-Object byte[]($len)
	$rng.GetBytes($bytes)

	$data = New-Object char[]($len)
	
	for ($i = 0 ; $i -lt $len ; $i++) {
		$data[$i] = $charSet[$bytes[$i] % $charSet.Length]
	}

	return -join $data

}

$fileName = "appointments.csv"
"" | out-file $filename

$numLines = Get-Random -Minimum 2 -Maximum 20
1..$numLines |% {
	$len = Get-Random -Minimum 10 -Maximum 256
	Get-RandomLine $len | out-file -Append $fileName
}
