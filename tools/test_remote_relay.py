#!/usr/bin/env python3
import importlib.util
import pathlib
import socket
import socketserver
import tempfile
import threading
import unittest

path = pathlib.Path(__file__).resolve().parents[1] / 'TournamentBot/deploy/remote-relay.py'
spec = importlib.util.spec_from_file_location('relay', path)
relay = importlib.util.module_from_spec(spec)
spec.loader.exec_module(relay)


class RelayTests(unittest.TestCase):
    def test_non_tournament_dotnet_calls_are_not_intercepted(self):
        self.assertIsNone(relay.server_arguments(['/app/OpenRA.Utility.dll', 'ca', '--replay-metadata']))
        self.assertIsNone(relay.server_arguments(['/app/OpenRA.Server.dll', 'Server.Name=Public server']))
        self.assertIsNone(relay.server_arguments(['/app/OpenRA.Server.dll', 'Server.Name=YMCA Tournament ../../etc']))

    def test_tournament_arguments_preserve_names_and_passwords(self):
        args = ['/app/game/engine/bin/OpenRA.Server.dll', 'Server.Name=YMCA Tournament M0068',
                'Server.Password=abc_DEF-123456789', 'Server.AllowedPlayerNames=Player One,Player Two']
        mid, values = relay.server_arguments(args)
        self.assertEqual(mid, 'M0068')
        self.assertEqual(values['Server.Password'], 'abc_DEF-123456789')
        self.assertEqual(values['Server.AllowedPlayerNames'], 'Player One,Player Two')

    def test_tcp_forwarding_is_byte_exact_in_both_directions(self):
        class Echo(socketserver.BaseRequestHandler):
            def handle(self):
                self.request.sendall(b'handshake\x00\xff')
                while True:
                    data = self.request.recv(65536)
                    if not data:
                        return
                    self.request.sendall(data)
        with socketserver.ThreadingTCPServer(('127.0.0.1',0),Echo) as upstream:
            threading.Thread(target=upstream.serve_forever,daemon=True).start()
            with relay.Forwarder(0,upstream.server_address) as forwarder:
                threading.Thread(target=forwarder.serve_forever,daemon=True).start()
                with socket.create_connection(('127.0.0.1',forwarder.server_address[1]),timeout=3) as client:
                    self.assertEqual(client.recv(100), b'handshake\x00\xff')
                    payload=bytes(range(256))*200
                    client.sendall(payload)
                    received=b''
                    while len(received)<len(payload):received+=client.recv(65536)
                    self.assertEqual(received,payload)
                forwarder.shutdown()
            upstream.shutdown()

    def test_marker_is_written_atomically_without_credentials(self):
        with tempfile.TemporaryDirectory() as d:
            p=pathlib.Path(d)/'remote-relay.json'
            relay.atomic_json(p,{'RequestId':'abc','HostId':'worlds'})
            self.assertTrue(p.exists())
            self.assertFalse(pathlib.Path(str(p)+'.relay-tmp').exists())

    def test_cross_host_api_redirect_is_rejected(self):
        with self.assertRaises(relay.FatalError):
            relay.NoRedirect().redirect_request(None,None,302,'',{},'https://other.example/')


if __name__=='__main__':unittest.main()
