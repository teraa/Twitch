using System.Runtime.InteropServices;

string msg = "hello👍";
var charSpan = msg.AsSpan();
var byteSpan = MemoryMarshal.AsBytes<char>(charSpan);
// https://discord.com/channels/143867839282020352/312132327348240384/937855316945666048

return;
