# plrm-chat
client-server TCP chat

server on localhost:5000

### TODO
1. Надо поправить формат сообщение от сервера и от клиента
Что-то вроде: https://stackoverflow.com/a/11762337/13982463
Это исправит ошибку с сериализацией потока сообщений, где я поставил `Task.Delay(10)`

2. Изменить обработчик сообщений от клиента/сервера, каждое сообщение будет иметь `enum` команды.
