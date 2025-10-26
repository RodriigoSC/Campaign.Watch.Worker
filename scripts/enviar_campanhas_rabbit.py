import pika
import json
import re

# --- Configurações ---
RABBITMQ_HOST = '192.168.0.3'
RABBITMQ_PORT = 5672
RABBITMQ_USER = 'admin'
RABBITMQ_PASS = 'admin123'
RABBITMQ_VHOST = 'monitoring'
QUEUE_NAME = 'MONITORING_ACCAMARGO'
EXCHANGE_NAME = 'MONITOR'  

object_id_list = [
    'ObjectId("68275978cc446d13190caf80")',
    'ObjectId("682b3cf8cc446d13190cc13f")',
    'ObjectId("68484a6a31243a261fb54b15")',
    'ObjectId("684b23c331243a261fb56371")',
    'ObjectId("684b268631243a261fb563e3")',
    'ObjectId("684b294431243a261fb56472")',
    'ObjectId("685aafda3d284727863645ff")',
    'ObjectId("685ab14e3d2847278636462b")',
    'ObjectId("685abf8b3d284727863647a8")',
    'ObjectId("685abfb43d284727863647b5")',
    'ObjectId("685aef3b3d28472786364b16")',
    'ObjectId("685af0d63d28472786364b34")',
    'ObjectId("685c46733d28472786365557")',
    'ObjectId("685d5949f133004f4c0d4d8c")',
    'ObjectId("685db0abf133004f4c0d54a0")',
    'ObjectId("6862acf7f133004f4c0d6a8a")',
    'ObjectId("6862ad43f133004f4c0d6a9b")',
    'ObjectId("6862ad97f133004f4c0d6aaa")',
    'ObjectId("6862ada3f133004f4c0d6aac")',
    'ObjectId("6862adbef133004f4c0d6ab7")',
    'ObjectId("6862aeb5f133004f4c0d6ad2")',
    'ObjectId("6862af32f133004f4c0d6ad7")',
    'ObjectId("6862af5df133004f4c0d6ad9")',
    'ObjectId("6862afa0f133004f4c0d6ae4")',
    'ObjectId("6862b021f133004f4c0d6aed")',
    'ObjectId("6862b14af133004f4c0d6b0c")',
    'ObjectId("6862b281f133004f4c0d6b3f")',
    'ObjectId("6862b326f133004f4c0d6b5a")',
    'ObjectId("6862e3b0f133004f4c0d6e3c")',
    'ObjectId("6862fd25f133004f4c0d719e")',
    'ObjectId("6862ff8bf133004f4c0d71a8")',
    'ObjectId("68643aabf133004f4c0d795f")',
    'ObjectId("6864490df133004f4c0d7b68")',
    'ObjectId("68644eacf133004f4c0d7bd3")',
    'ObjectId("68644ecaf133004f4c0d7bd4")',
    'ObjectId("68644fdcf133004f4c0d7c18")',
    'ObjectId("68657884f133004f4c0d8444")',
    'ObjectId("68657901f133004f4c0d844e")',
    'ObjectId("68667ed0f133004f4c0d88ea")',
    'ObjectId("686683c1f133004f4c0d898c")',
    'ObjectId("68669ce2f133004f4c0d8c5e")',
    'ObjectId("6866cbb7f133004f4c0d8ff6")',
    'ObjectId("6866ce49f133004f4c0d900b")',
    'ObjectId("6867d5c6f133004f4c0d9529")',
    'ObjectId("68682284f133004f4c0d9daf")',
    'ObjectId("686fba0af133004f4c0dbfcf")',
    'ObjectId("686fbca8f133004f4c0dc005")',
    'ObjectId("686fc11af133004f4c0dc0b4")',
    'ObjectId("68701b67f133004f4c0dca66")',
    'ObjectId("68701e0ff133004f4c0dca8b")',
    'ObjectId("68710bf3f133004f4c0dce6b")',
    'ObjectId("68711ea1f133004f4c0dd1f7")',
    'ObjectId("68711ee4f133004f4c0dd214")',
    'ObjectId("68764f7cce6b07c65f10e6ae")',
    'ObjectId("6878dcf820143a77ff945905")',
    'ObjectId("687959a820143a77ff9462f6")',
    'ObjectId("687e265d20143a77ff9470ef")',
    'ObjectId("687f73da20143a77ff947bde")',
    'ObjectId("687f73de20143a77ff947be3")',
    'ObjectId("687f80a920143a77ff947c41")',
    'ObjectId("687f8b5e20143a77ff947dbc")',
    'ObjectId("6880c01420143a77ff9484ee")',
    'ObjectId("6880c09720143a77ff9484f0")',
    'ObjectId("6880dc5720143a77ff948631")',
    'ObjectId("6882278e20143a77ff94906e")',
    'ObjectId("68822f9520143a77ff94916e")',
    'ObjectId("6882493220143a77ff94948d")',
    'ObjectId("68827fc920143a77ff94983f")',
    'ObjectId("68837d7420143a77ff949fe5")',
    'ObjectId("68838f6d20143a77ff94a23c")',
    'ObjectId("6887625120143a77ff94ac2d")',
    'ObjectId("6887d0f320143a77ff94b3ba")',
    'ObjectId("68891f4720143a77ff94be05")',
    'ObjectId("688b6dce20143a77ff94d1d5")',
    'ObjectId("688cfc0d20143a77ff94e2a9")',
    'ObjectId("6890994b20143a77ff94ea2f")',
    'ObjectId("68909ae420143a77ff94ea31")',
    'ObjectId("6890b2c320143a77ff94eb32")',
    'ObjectId("6890c2fb20143a77ff94eced")',
    'ObjectId("6890cccc20143a77ff94edb2")',
    'ObjectId("6890cdcd20143a77ff94edb7")',
    'ObjectId("6891dc8220143a77ff94f36b")',
    'ObjectId("6891fbe420143a77ff94f3c2")',
    'ObjectId("6891fd0620143a77ff94f3d6")',
    'ObjectId("689355d720143a77ff95008a")',
    'ObjectId("6893945420143a77ff9505a8")',
    'ObjectId("6894a91b20143a77ff950cca")',
    'ObjectId("6895e1da20143a77ff9517b3")',
    'ObjectId("6896546420143a77ff9527bc")',
    'ObjectId("689904be20143a77ff952a1b")',
    'ObjectId("689908ef20143a77ff952a23")',
    'ObjectId("68990cf520143a77ff952a32")',
    'ObjectId("6899d60f20143a77ff952b5c")',
    'ObjectId("6899d71220143a77ff952b6b")',
    'ObjectId("689a9f5a20143a77ff953651")',
    'ObjectId("689cc3a820143a77ff9546f8")',
    'ObjectId("689f2ee120143a77ff955a6b")',
    'ObjectId("68a30bf920143a77ff95649b")',
    'ObjectId("68a35a0420143a77ff956a58")',
    'ObjectId("68a468fb20143a77ff95713d")'
]

def extract_id(object_id_str):
    match = re.search(r'\"(.+?)\"', object_id_str)
    if match:
        return match.group(1)
    return None

def send_to_queue():
    connection = None
    try:
        campaign_ids = [extract_id(oid) for oid in object_id_list]
        campaign_ids = [cid for cid in campaign_ids if cid]

        # Configura a conexão
        credentials = pika.PlainCredentials(RABBITMQ_USER, RABBITMQ_PASS)
        params = pika.ConnectionParameters(
            host=RABBITMQ_HOST,
            port=RABBITMQ_PORT,
            virtual_host=RABBITMQ_VHOST,
            credentials=credentials
        )
        connection = pika.BlockingConnection(params)
        channel = connection.channel()

        # Garante que a fila exista e seja durável
        channel.queue_declare(queue=QUEUE_NAME, durable=True)

        print(f"[*] Conectado ao RabbitMQ. Enviando {len(campaign_ids)} mensagens para a fila '{QUEUE_NAME}'...")

        for campaign_id in campaign_ids:
            message_body = json.dumps(campaign_id)

            channel.basic_publish(
                exchange='',             
                routing_key=QUEUE_NAME,   
                body=message_body,
                properties=pika.BasicProperties(
                    delivery_mode=2,  
                )
            )
            print(f" [x] Enviado: {message_body}")

        print(f"\n[*] Todas as {len(campaign_ids)} mensagens foram enviadas com sucesso.")

    except pika.exceptions.AMQPConnectionError as e:
        print(f"ERRO: Não foi possível conectar ao RabbitMQ em {RABBITMQ_HOST}:{RABBITMQ_PORT}")
        print("Verifique se o RabbitMQ está rodando (docker ps) e se as credenciais (admin/admin123) estão corretas.")
    except Exception as e:
        print(f"Ocorreu um erro inesperado: {e}")
    finally:
        if connection and not connection.is_closed:
            connection.close()
            print("[*] Conexão com o RabbitMQ fechada.")

if __name__ == '__main__':
    send_to_queue()