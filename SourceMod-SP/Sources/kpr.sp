#pragma semicolon 0
#pragma tabsize 0
#pragma newdecls required

#include <sourcemod>
#include <ripext>
#include <clientmod>
#include <clientmod/multicolors>

#define PLUGIN_VERSION "1.0"
#define API_URL "http://css.ktvcss.org.ru/"
#define SZF(%0) %0, sizeof(%0)

HTTPClient g_hHTTPClient

public Plugin myinfo = 
{
	name = "kTVCSS Rankings",
	author = "INZAME && w0key",
	description = "kTVCSS Community rankings plugin",
	version = PLUGIN_VERSION,
	url = "https://vk.com/ktvcss"
}

public void OnPluginStart()
{
	g_hHTTPClient = new HTTPClient(API_URL)
	RegConsoleCmd("rank", g_DisplayRanks)
}	

public void OnClientPostAdminCheck(int iClient)
{
	OnDataPush(iClient)
}

public Action g_DisplayRanks(int iClient, int iArgs) {
	OnDataPush(iClient)
}

void OnDataPush(int iClient){
	char szAuth[32], szQuery[64]
	GetClientAuthId(iClient, AuthId_Steam2, szAuth, 30, true)
	
	FormatEx(SZF(szQuery), "kpr.php?token=t0k3n&id=%s", szAuth)
	g_hHTTPClient.Get(szQuery, OnDataReceived, GetClientUserId(iClient))
}

public void OnDataReceived(HTTPResponse Response, any iUserId, const char[] szError){
	if (szError[0] != 0) {
		SetFailState("Error: Response %s", szError)
		return
	}

	int iClient = GetClientOfUserId(iUserId)
	if(!iClient) {
		return
	}
	
	JSONObject hJSObj = view_as<JSONObject>(Response.Data)
	char szPlace[8], szTotalKills[8], szTotalDeaths[8], szKD[8], szRankName[16], szRankPts[8], szMatchesPlayed[8], szMatchesVic[8], szMatchesDef[8]
	
	hJSObj.GetString("Place", szPlace, sizeof(szPlace))
	hJSObj.GetString("Kills", szTotalKills, sizeof(szTotalKills))
	hJSObj.GetString("Deaths", szTotalDeaths, sizeof(szTotalDeaths))
	hJSObj.GetString("KD", szKD, sizeof(szKD))
	hJSObj.GetString("RankName", szRankName, sizeof(szRankName))
	hJSObj.GetString("RankPTS", szRankPts, sizeof(szRankPts))
	hJSObj.GetString("MatchesPlayed", szMatchesPlayed, sizeof(szMatchesPlayed))
	hJSObj.GetString("MatchesVictories", szMatchesVic, sizeof(szMatchesVic))
	hJSObj.GetString("MatchesDefeats", szMatchesDef, sizeof(szMatchesDef))
	
	CPrintToChatAll("{cyan}*-*-*-*-*- [kTVCSS Players Rankings] -*-*-*-*-*")
	CPrintToChatAll("{cyan}* %N {yellow}place {green}%s. {yellow}Rank is {blue}%s (%s)", iClient, szPlace, szRankName, szRankPts)
	CPrintToChatAll("{cyan}* {yellow}KD: {green}%s. {yellow}Kills: {legendary}%s. {yellow}Deaths: {ancient}%s", szKD, szTotalKills, szTotalDeaths)
	CPrintToChatAll("{cyan}* {yellow}Total played: {gray}%s. {yellow}Wins: {green}%s. {yellow}Defeats: {maroon}%s", szMatchesPlayed, szMatchesVic, szMatchesDef)
	CPrintToChatAll("{cyan}*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*")
	delete hJSObj
}