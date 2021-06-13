import vk_api
import sys
from datetime import datetime
import mysql.connector

DEBUG = False
epoch = datetime.utcfromtimestamp(0)
mydb = mysql.connector.connect(host="localhost", user="root", database="ktvcss")
mycursor = mydb.cursor()


def random_id():
    dt = datetime.now()
    return (dt - epoch).total_seconds() * 1000.0


class Sender:

    def __init__(self, api_token):
        vk_session = vk_api.VkApi(token=api_token)
        self.vk = vk_session.get_api()
        self.vk_id = None

    def get_vkid(self, steam_id):
        mycursor.execute("SELECT `VkID` FROM `players` WHERE `SteamID` = \"%s\"" % steam_id)
        myresult = mycursor.fetchall()
        for x in myresult:
            if x[0]:
                if DEBUG: print('VkID: %s' % x[0])
                self.vk_id = x[0]
                return x[0]
            else:
                if DEBUG: print('VkID by %s not found' % steam_id)
                return None

    def recieve_stats(self, vk_id):
        nickname, kills, deaths, headshots, kd, rank, sendenabled, date, wins, loses, pts, position = [None] * 12
        name = self.vk.users.get(user_ids=vk_id)[0]['first_name']
        mycursor.execute("SELECT `SendStatistics` FROM `players` WHERE `VkID` = \"%s\" AND `SteamID` = \"%s\"" % (
            vk_id, sys.argv[1]))
        myresult = mycursor.fetchall()
        for x in myresult:
            sendenabled = x[0]

        if sendenabled == 1:
            mycursor.execute(
                "SELECT * FROM `players` WHERE `VkID` = \"%s\" AND `SteamID` = \"%s\"" % (vk_id, sys.argv[1]))
            myresult = mycursor.fetchall()
            for x in myresult:
                nickname = x[1]
                kills = x[3]
                deaths = x[4]
                headshots = x[5]
                kd = x[6]
                pts = x[8]
                rank = x[9]
                wins = x[11]
                loses = x[12]
                date = x[14].strftime("%H:%M от %d.%m.%Y")

            mycursor.execute("SELECT * FROM `players` ORDER BY `RankPTS` DESC")
            myresult = mycursor.fetchall()
            for x in myresult:
                if x[2] == sys.argv[1]:
                    position = myresult.index(x) + 1
                    if DEBUG: print(x, position)
            header = f'{name} \"{nickname}\" ваша статистика.\nПоследний матч в {date}.\n'
            kill_stats = f'Убийств: {kills}. Смертей: {deaths}. Хэдшотов: {headshots}. K/D: {kd}.\n'
            play_stats = f'Побед: {wins}. Поражений {loses}.\nТекущий рейтинг: {pts}. Позиция в рейтинге: {position}\n'
            rank_footer = f'Ваш текущий ранк: {rank}'
            final_stats = header + kill_stats + play_stats + rank_footer
            if DEBUG: print(final_stats)
            return final_stats
        else:
            if DEBUG: print('User %s doesn\'t want to receive stats! Or user doesn\'t exists!' % sys.argv[1])
            return None

    def send_message(self, message, vk_id):
        self.vk.messages.send(message=message, user_id=vk_id, random_id=random_id())


def main():
    if len(sys.argv) >= 2:
        steam_id = sys.argv[1]
        with open('./token', 'r') as token_file:
            token = token_file.readline()
        sender = Sender(token)
        vk_id = sender.get_vkid(steam_id)
        if vk_id:
            message = sender.recieve_stats(vk_id)
            if message:
                sender.send_message(message, vk_id)
                print('Statistics sent to %s' % vk_id)
    else:
        print('Usage: send_stats.py <STEAM_ID>')


if __name__ == '__main__':
    main()
