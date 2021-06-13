import vk_api
from vk_api.longpoll import VkLongPoll, VkEventType
from datetime import datetime
import re
import mysql.connector
from time import sleep

admins = []

class Bot:

    def __init__(self, api_token):
        vk_session = vk_api.VkApi(token=api_token)
        self.longpoll = VkLongPoll(vk_session)
        self.vk = vk_session.get_api()
        self.event = None
        self.mydb = None
        self.mycursor = None
        print("Started kTVCSS-Ranking-System-Bot")

    def random_id(self):
        dt = datetime.now()
        epoch = datetime.utcfromtimestamp(0)
        return (dt - epoch).total_seconds() * 1000.0
    
    def connect(self):
        self.mydb = mysql.connector.connect(host="localhost", user="root", password="", database="")
        self.mycursor = self.mydb.cursor()

    def disconnect(self):
        self.mydb.close()

    def resetid(self):
        self.connect()
        user_id = self.event.user_id
        try:
            steamid = re.search(r"(!\w+) (STEAM_\d:\d:\d+)", self.event.text).group(2).strip()
        except Exception:
            self.vk.messages.send(message='Введенный STEAM_ID некорректный.\nПример: !resetid STEAM_0:0:0123456789',
                                  user_id=user_id, random_id=self.random_id())
        else:
            self.mycursor.execute("SELECT `VkID` FROM `players` WHERE `SteamID` = \"%s\"" % steamid)
            myresult = self.mycursor.fetchall()
            for x in myresult:
                vk_id = x[0]
            self.mycursor.execute("UPDATE `players` SET `VkID`=\"\" WHERE `SteamID`=\"%s\";" % steamid)
            self.mydb.commit()
            self.vk.messages.send(message=f'{steamid} был отвязан от пользователя {vk_id}',
                                  user_id=user_id, random_id=self.random_id())
            print('Admin %s used !resetid for %s' % (str(user_id), steamid))
        finally:
            self.disconnect()
            
    def resetid_chat(self):
        self.connect()
        user_id = self.event.user_id
        chat_id = self.event.chat_id
        try:
            steamid = re.search(r"(!\w+) (STEAM_\d:\d:\d+)", self.event.text).group(2).strip()
        except Exception:
            self.vk.messages.send(message='Введенный STEAM_ID некорректный.\nПример: !resetid STEAM_0:0:0123456789',
                                  chat_id=chat_id, random_id=self.random_id())
        else:
            self.mycursor.execute("SELECT `VkID` FROM `players` WHERE `SteamID` = \"%s\"" % steamid)
            myresult = self.mycursor.fetchall()
            for x in myresult:
                vk_id = x[0]
            self.mycursor.execute("UPDATE `players` SET `VkID`=\"\" WHERE `SteamID`=\"%s\";" % steamid)
            self.mydb.commit()
            self.vk.messages.send(message=f'{steamid} был отвязан от пользователя {vk_id}',
                                  chat_id=chat_id, random_id=self.random_id())
            print('Admin %s used !resetid for %s' % (str(user_id), steamid))
        finally:
            self.disconnect()

    def getinfo(self):
        self.connect()
        name = None
        lastname = None
        nickname = None
        steamid = ''
        vk_id = ''
        user_id = self.event.user_id
        splitted = self.event.text.split(' ')
        if len(splitted) <= 1:
            self.vk.messages.send(
                message='!getinfo применяется для получения информации о пользователе по STEAM ID, VK ID или VK '
                        'Screen name\nПримеры:\nУзнать информацию по STEAM ID: !getinfo STEAM_0:0:0123456789\nУзнать '
                        'информацию по VK ID: !getinfo 459551868\nУзнать информацию по VK Screen name: !getinfo '
                        'inzame',
                user_id=user_id, random_id=self.random_id())
        else:
            if "STEAM_" in splitted[1]:
                try:
                    steamid = re.search(r"(!\w+) (STEAM_\d:\d:\d+)", self.event.text).group(2).strip()
                except Exception:
                    self.vk.messages.send(
                        message='Введенный STEAM_ID некорректный.\nПример: !getinfo STEAM_0:0:0123456789',
                        user_id=user_id, random_id=self.random_id())
                else:
                    self.mycursor.execute("SELECT `VkID` FROM `players` WHERE `SteamID` = \"%s\"" % steamid)
                    myresult = self.mycursor.fetchall()
                    if len(myresult) >= 1:
                        for x in myresult:
                            vk_id = x[0]
                    if vk_id != '':
                        user = self.vk.users.get(user_ids=vk_id)[0]
                        name = user['first_name']
                        lastname = user['last_name']

                    self.mycursor.execute("SELECT `Name` FROM `players` WHERE `SteamID` = \"%s\"" % steamid)
                    myresult = self.mycursor.fetchall()
                    for x in myresult:
                        nickname = x[0]
                    self.vk.messages.send(
                        message=f'Пользователь: {name} {lastname}\nНикнейм: {nickname}\nVK ID: {vk_id}\nSTEAM ID: {steamid}',
                        user_id=user_id, random_id=self.random_id())
            else:
                try:
                    vk_id = re.search(r"(!\w+) ([a-z0-9]*)|(\d*)", self.event.text).group(2).strip()
                except Exception:
                    self.vk.messages.send(
                        message='Введенный VK_ID/Screen_Link некорректный.\nПример: !infoabout inzame ИЛИ !infoabout 459551868',
                        user_id=user_id, random_id=self.random_id())
                else:
                    user = self.vk.users.get(user_ids=vk_id)[0]
                    numeric_id = user['id']
                    name = user['first_name']
                    lastname = user['last_name']

                    self.mycursor.execute("SELECT `SteamID` FROM `players` WHERE `VkID` = \"%s\"" % numeric_id)
                    myresult = self.mycursor.fetchall()
                    for x in myresult:
                        steamid = x[0]
                    if steamid != '':
                        self.mycursor.execute("SELECT `Name` FROM `players` WHERE `SteamID` = \"%s\"" % steamid)
                        myresult = self.mycursor.fetchall()
                        for x in myresult:
                            nickname = x[0]
                        self.vk.messages.send(
                            message=f'Пользователь: {name} {lastname}\nНикнейм: {nickname}\nVK ID: {numeric_id}\nSTEAM ID: {steamid}',
                            user_id=user_id, random_id=self.random_id())
                    else:
                        self.vk.messages.send(message=f'Пользователь {vk_id} не привязал свою страницу.',
                                              user_id=user_id, random_id=self.random_id())
        self.disconnect()

    def rmplayer(self):
        self.connect()
        ids_string = ''
        players = []
        steamid = None
        user_id = self.event.user_id
        self.mycursor.execute(f"SELECT Name FROM ktvcss.teams WHERE CapID = \"{user_id}\"")
        myresult = self.mycursor.fetchall()
        for x in myresult:
            if not x[0]:
                return None
        try:
            steamid = re.search(r"(!\w+) (STEAM_\d:\d:\d+)", self.event.text).group(2).strip()
        except Exception:
            self.vk.messages.send(message='Введенный STEAM_ID некорректный.\nПример: !rm STEAM_0:0:0123456789',
                                  user_id=user_id, random_id=self.random_id())
            return None
        self.mycursor.execute(f"SELECT Name FROM ktvcss.teams WHERE Staff LIKE '%{steamid}%'")
        myresult = self.mycursor.fetchall()
        if len(myresult) == 0:
            self.vk.messages.send(message=f"STEAM ID: {steamid} не состоит в команде!",
                                  user_id=user_id, random_id=self.random_id())
            self.disconnect()
            return None
        self.mycursor.execute(f"SELECT Staff FROM ktvcss.teams WHERE CapID = \"{user_id}\"")
        myresult = self.mycursor.fetchall()
        for x in myresult:
            if x[0]:
                players = x[0].strip().split(";")
                for item in players:
                    if item == '':
                        players.pop(players.index(item))
        if steamid in players:
            print(players)
            players.remove(steamid)
            print(players)
        else:
            self.vk.messages.send(message="Такого игрока нет в команде!",
                                  user_id=self.event.user_id, random_id=self.random_id())
            return None
        for id in players:
            ids_string += f'{id};'
        self.mycursor.execute(f"UPDATE ktvcss.teams SET Staff=\"{ids_string}\" WHERE `CapID`=\"{user_id}\"")
        self.mydb.commit()
        self.vk.messages.send(message=f"Игрок с SteamID: {steamid} удален из вашей команды!",
                              user_id=self.event.user_id, random_id=self.random_id())
        self.disconnect()

    def leaveteam(self):
        players = []
        ids_string = ''
        steamid = None
        self.connect()
        user_id = self.event.user_id
        self.mycursor.execute(f"SELECT SteamID FROM ktvcss.players WHERE VkID = \"{user_id}\"")
        myresult = self.mycursor.fetchall()
        if len(myresult) == 0:
            self.vk.messages.send(message=f"Ваша страница не связана со Steam ID",
                              user_id=self.event.user_id, random_id=self.random_id())
            self.disconnect()
            return False          
        for x in myresult:
            if x[0]:
                steamid = x[0]
                self.mycursor.execute(f"SELECT Staff FROM ktvcss.teams WHERE Staff LIKE '%{steamid}%'")
                staff = self.mycursor.fetchall()
                for x in staff:
                    if x[0]:
                        players = x[0].strip().split(";")
                        for item in players:
                            if item == '':
                                players.pop(players.index(item))
        if steamid in players:
            players.remove(steamid)
            for id in players:
                ids_string += f'{id};'
            self.mycursor.execute(f"UPDATE ktvcss.teams SET Staff=\"{ids_string}\" WHERE Staff LIKE '%{steamid}%'")
            self.mydb.commit()
            self.vk.messages.send(message=f"Вы вышли из команды!",
                                  user_id=self.event.user_id, random_id=self.random_id())
        else:
            self.vk.messages.send(message="Данный игрок не в команде",
                                  user_id=self.event.user_id, random_id=self.random_id())        
        self.disconnect()
        

    def addplayer(self):
        self.connect()
        ids_string = ''
        players = []
        steamid = None
        user_id = self.event.user_id
        self.mycursor.execute(f"SELECT Name FROM ktvcss.teams WHERE CapID = \"{user_id}\"")
        myresult = self.mycursor.fetchall()
        if len(myresult) == 0:
            return None
        try:
            steamid = re.search(r"(!\w+) (STEAM_\d:\d:\d+)", self.event.text).group(2).strip()
        except Exception:
            self.vk.messages.send(message='Введенный STEAM_ID некорректный.\nПример: !addplayer STEAM_0:0:0123456789',
                                  user_id=user_id, random_id=self.random_id())
            return None
        self.mycursor.execute(f"SELECT Name FROM ktvcss.teams WHERE Staff LIKE '%{steamid}%'")
        myresult = self.mycursor.fetchall()
        for x in myresult:
            if x[0]:
                self.vk.messages.send(message=f"STEAM ID: {steamid} уже состоит в команде {x[0]}!",
                                      user_id=user_id, random_id=self.random_id())
                return None
        self.mycursor.execute(f"SELECT Staff FROM ktvcss.teams WHERE CapID = \"{user_id}\"")
        myresult = self.mycursor.fetchall()
        for x in myresult:
            if x[0]:
                players = x[0].strip().split(";")
                for item in players:
                    if item == '':
                        players.pop(players.index(item))

        players.append(steamid)
        for id in players:
            ids_string += f'{id};'
        self.mycursor.execute(f"UPDATE ktvcss.teams SET Staff=\"{ids_string}\" WHERE `CapID`=\"{user_id}\"")
        self.mydb.commit()
        self.vk.messages.send(message=f"Игрок с SteamID: {steamid} добавлен в вашу команду!",
                              user_id=self.event.user_id, random_id=self.random_id())
        self.disconnect()

    def delteam(self):
        self.connect()
        user_id = self.event.user_id
        self.mycursor.execute(f"SELECT Name FROM ktvcss.teams WHERE CapID = \"{user_id}\"")
        myresult = self.mycursor.fetchall()
        for x in myresult:
            if x[0]:
                self.mycursor.execute(f"DELETE FROM ktvcss.teams WHERE CapID = \"{user_id}\"")
                self.mydb.commit()
                self.vk.messages.send(message=f"Ваша команда {x[0]} удалена!", user_id=user_id, random_id=self.random_id())
        self.disconnect()

    def regteam(self):
        self.connect()
        repeat_validator = []
        ids_validated = []
        ids_string = ""
        user_id = self.event.user_id
        splitted = self.event.text.split(' ')
        if len(splitted) <= 1:
            self.vk.messages.send(
                message='Пример:\n!regteam KtvcssTeamName; STEAM_0:1:321321321; STEAM_0:1:123123123; STEAM_0:1:123456789',
                user_id=user_id,
                random_id=self.random_id())
            return None
        try:
            regex_string = re.search(r"(!\w+) ([a-zA-Z0-9_\s;*.<>!@#$()`\\\&+=\[\]\"\']+)[;](.+)", self.event.text)
        except Exception:
            self.vk.messages.send(message='Такое название команды не допустимо!', user_id=user_id,
                                  random_id=self.random_id())
        else:
            teamname = str(regex_string.group(2))
            steam_ids = regex_string.group(3).split(';')
            for steam_id in steam_ids:
                if steam_id not in repeat_validator:
                    repeat_validator.append(steam_id)
                else:
                    self.vk.messages.send(message="У вас есть повторяющиеся Steam ID!", user_id=user_id,
                                          random_id=self.random_id())
                    return None
            self.mycursor.execute(f"SELECT Name FROM ktvcss.teams WHERE CapID = \"%s\"" % user_id)
            myresult = self.mycursor.fetchall()
            for x in myresult:
                if x[0]:
                    self.vk.messages.send(message=f"Вы уже явлеетесь администратором команды {x[0]}!",
                                          user_id=user_id, random_id=self.random_id())
                    return None
            self.mycursor.execute(f"SELECT Name FROM ktvcss.teams WHERE Name LIKE '%{teamname}%'")
            myresult = self.mycursor.fetchall()
            for x in myresult:
                if x[0]:
                    self.vk.messages.send(message=f"Команда {x[0]} уже зарегистрирована!",
                                          user_id=user_id, random_id=self.random_id())
                    return None
            for steam_id in steam_ids:
                stripped_id = steam_id.strip()
                if not re.search(r"STEAM_\d:\d:\d+", stripped_id):
                    self.vk.messages.send(message=f'STEAM ID: {stripped_id} не корректный!', user_id=user_id,
                                          random_id=self.random_id())
                    return None
                else:
                    self.mycursor.execute(f"SELECT Name FROM ktvcss.teams WHERE Staff LIKE '%{stripped_id}%'")
                    myresult = self.mycursor.fetchall()
                    for x in myresult:
                        if x[0]:
                            self.vk.messages.send(message=f"STEAM ID: {stripped_id} уже состоит в команде {x[0]}!",
                                                  user_id=user_id, random_id=self.random_id())
                            return None
                    ids_validated.append(stripped_id)
            for id in ids_validated:
                ids_string += f'{id};'
            sql = "INSERT INTO ktvcss.teams (Name, Staff, CapID) VALUES (%s, %s, %s)"
            values = (teamname, ids_string, user_id)
            self.mycursor.execute(sql, values)
            self.mydb.commit()
            self.vk.messages.send(message=f'Ваша команда {teamname} зарегистрирована.', user_id=user_id,
                                  random_id=self.random_id())
        self.disconnect()

    def help(self):
        self.connect()
        user_id = self.event.user_id
        name = self.vk.users.get(user_ids=self.event.user_id)[0]['first_name']
        self.vk.messages.send(
            message="Здравствуйте, %s, для вас доступны следующие команды: !help, !setid, !delid, !mystats, !togglestats, !regteam, !leaveteam" % name,
            user_id=user_id, random_id=self.random_id())
        if str(user_id) in admins:
            self.vk.messages.send(
                message='Так как вы администратор, для вас доступны дополнительные команды: !resetid, !getinfo',
                user_id=user_id, random_id=self.random_id())
        self.mycursor.execute("SELECT Name FROM ktvcss.teams WHERE CapID = \"%s\"" % user_id)
        myresult = self.mycursor.fetchall()
        for x in myresult:
            for x in myresult:
                if x[0]:
                    self.vk.messages.send(
                        message="Так как вы являетесь администратором команды, для вас так же доступны команды: !delteam, !addplayer, !rmplayer",
                        user_id=user_id, random_id=self.random_id())
        self.disconnect()

    def togglestats(self):
        self.connect()
        user_id = self.event.user_id
        name = self.vk.users.get(user_ids=self.event.user_id)[0]['first_name']
        self.mycursor.execute("SELECT `SendStatistics` FROM `players` WHERE `VkID` = \"%s\"" % user_id)
        myresult = self.mycursor.fetchall()
        for x in myresult:
            if x[0] == 1:
                self.mycursor.execute("UPDATE `players` SET `SendStatistics`=\"0\" WHERE `VkID`=\"%s\";" % user_id)
                self.vk.messages.send(message='%s, для вас были отключены уведомления.' % name,
                                      user_id=self.event.user_id, random_id=self.random_id())
            else:
                self.mycursor.execute("UPDATE `players` SET `SendStatistics`=\"1\" WHERE `VkID`=\"%s\";" % user_id)
                self.vk.messages.send(message='%s, для вас были включены уведомления.' % name,
                                      user_id=self.event.user_id, random_id=self.random_id())
        self.mydb.commit()
        self.disconnect()

    def delid(self):
        self.connect()
        user_id = self.event.user_id
        name = self.vk.users.get(user_ids=self.event.user_id)[0]['first_name']
        self.mycursor.execute("UPDATE `players` SET `VkID`=\"\" WHERE `VkID`=\"%s\";" % user_id)
        self.mydb.commit()
        self.vk.messages.send(message='%s, вы отвязали свою страницу.' % name,
                              user_id=self.event.user_id, random_id=self.random_id())
        self.disconnect()

    def setid(self):
        self.connect()
        user_id = self.event.user_id
        splitted = self.event.text.split(' ')
        if len(splitted) <= 1:
            self.vk.messages.send(
                message='!setid привяжет вашу страницу к Steam ID.\nНапример: !setid STEAM_0:0:0123456789',
                user_id=user_id, random_id=self.random_id())
            return None
        try:
            steamid = re.search(r"(!\w+) (STEAM_\d:\d:\d+)", self.event.text).group(2).strip()
        except Exception:
            self.vk.messages.send(message='Введенный STEAM_ID некорректный.\nПример: !setid STEAM_0:0:0123456789',
                                  user_id=user_id, random_id=self.random_id())
            return None
        self.mycursor.execute("SELECT `SteamID` FROM `players` WHERE `VkID` = \"%s\"" % user_id)
        myresult = self.mycursor.fetchall()
        for x in myresult:
            if x[0]:
                self.vk.messages.send(
                    message="Ваш VK_ID %s уже связан с %s.\nДля перепривязки аккаунта напишите !delid" % (
                        user_id, x[0]),
                    user_id=user_id, random_id=self.random_id())
                return None
        self.mycursor.execute("SELECT `VkID` FROM `players` WHERE `SteamID` = \"%s\"" % steamid)
        myresult = self.mycursor.fetchall()
        if len(myresult) != 0:
            for x in myresult:
                if x[0] == '':
                    self.mycursor.execute("UPDATE `players` SET `VkID`=\"%s\" WHERE `SteamID`=\"%s\";" % (user_id, steamid))
                    self.mydb.commit()
                    self.vk.messages.send(
                        message='Ваш VK_ID %s был привязан к %s' % (user_id, steamid),
                        user_id=user_id, random_id=self.random_id())
                else:
                    self.vk.messages.send(
                        message='К этому Steam ID уже привязан VK!\nДля перепривязки аккаунта напишите администратору.',
                        user_id=user_id, random_id=self.random_id())
        else:
            self.vk.messages.send(
                message='Этот Steam ID не учитывается статистикой.\nСыграйте хотя бы одну игру.',
                user_id=user_id, random_id=self.random_id())
        self.disconnect()

    def mystats(self):
        self.connect()
        word = 'матч'
        nickname, kills, deaths, headshots, kd, rank, sendenabled, date, wins, loses, pts, position, vk_id, steam_id = [
                                                                                                                           None] * 14
        name = self.vk.users.get(user_ids=self.event.user_id)[0]['first_name']
        user_id = self.event.user_id
        self.mycursor.execute("SELECT * FROM `players` WHERE `VkID` = \"%s\"" % user_id)
        myresult = self.mycursor.fetchall()
        team_info = ''
        if len(myresult) >= 1:
            for x in myresult:
                nickname = x[1]
                steam_id = x[2]
                kills = x[3]
                deaths = x[4]
                headshots = x[5]
                kd = x[6]
                pts = x[8]
                rank = x[9]
                wins = x[11]
                loses = x[12]
                date = x[14].strftime("%H:%M от %d.%m.%Y")
                vk_id = x[15]
            self.mycursor.execute("SELECT * FROM `players` ORDER BY `RankPTS` DESC")
            myresult = self.mycursor.fetchall()
            total = wins + loses
            for x in myresult:
                if x[15] == vk_id:
                    position = myresult.index(x) + 1
            self.mycursor.execute(f"SELECT Name FROM ktvcss.teams WHERE Staff LIKE '%{steam_id}%'")
            myresult = self.mycursor.fetchall()
            if len(myresult) != 0:
                for x in myresult:
                    if x[0]:
                        team_info = f"Вы состоите в команде: {x[0]}\n"
            else:
                team_info = f"Вы не состоите в команде.\n"
            header = f'{name} \"{nickname}\" ваша статистика:\nПоследний матч в {date}.\n'
            kill_stats = f'Убийств: {kills}. Смертей: {deaths}. Хэдшотов: {headshots}. K/D: {kd}.\n'
            play_stats = f'Побед: {wins}. Поражений {loses}.\n'
            if int(total) >= 10:
                rank_footer = f'Текущий рейтинг: {pts}. Позиция в рейтинге: {position}\nВаш текущий ранк: {rank}\n'
            else:
                if int(10 - int(total)) == 1:
                    word = 'матч'
                elif int(10 - int(total)) in [2, 3, 4]:
                    word = 'матча'
                elif int(10 - int(total)) in [5, 6, 7, 8, 9]:
                    word = 'матчей'
                rank_footer = f'Вы проходите калибровку. До конца калибровки {10 - total} {word}.\n'
            final_message = header + kill_stats + play_stats + rank_footer + team_info
            self.vk.messages.send(message=final_message, user_id=user_id, random_id=self.random_id())
        else:
            header = f'{name}, не найден Steam ID связанный с этой страницей.\n'
            body = f'Привяжите вашу страницу командой !setid ВАШ_STEAM_ID'
            final_message = header + body
            self.vk.messages.send(message=final_message, user_id=user_id, random_id=self.random_id())
        self.disconnect()

    def start(self):
        for self.event in self.longpoll.listen():
            if self.event.type == VkEventType.MESSAGE_NEW and self.event.to_me and self.event.text:
                if self.event.from_user:
                    if self.event.text == '!help':
                        self.help()
                    elif self.event.text == '!togglestats':
                        self.togglestats()
                    elif self.event.text == '!mystats':
                        self.mystats()
                    elif self.event.text == '!delteam':
                        self.delteam()
                    elif self.event.text == '!leaveteam':
                        self.leaveteam()
                    elif self.event.text.startswith('!setid'):
                        self.setid()
                    elif self.event.text.startswith('!delid'):
                        self.delid()
                    elif self.event.text.startswith('!regteam'):
                        self.regteam()
                    elif self.event.text.startswith('!addplayer'):
                        self.addplayer()
                    elif self.event.text.startswith('!rmplayer'):
                        self.rmplayer()
                    elif self.event.text.startswith('!resetid') and str(self.event.user_id) in admins:
                        self.resetid()
                    elif self.event.text.startswith('!getinfo') and str(self.event.user_id) in admins:
                        self.getinfo()
                if self.event.from_chat:
                    if '!resetid' in self.event.text and str(self.event.user_id) in admins:
                        self.resetid_chat()

def main():
    with open('./token', 'r') as token_file:
        token = token_file.readline()
    with open('./admins', 'r') as admins_file:
        for line in admins_file:
            temp = line.split(' ')[0].rstrip()
            if temp != '': admins.append(temp)
    print('Admins loaded:', admins)
    while True:
        try:
            bot = Bot(token)
            bot.start()
            print('[DEBUG] Connected to VK_API!')
        except Exception as e:
            print('[ERROR] EXCEPTION CAUGHT! RESTARTING! ')
            print(e)


if __name__ == '__main__':
    main()
