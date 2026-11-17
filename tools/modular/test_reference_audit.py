import unittest
from audit_reference_changes import blocks


class ReferenceAuditTests(unittest.TestCase):
    def test_preserves_repeated_blocks_and_children(self):
        text = '\ufeffA:\n\tHealth:\n\t\tHP: 100\nB:\n\tTest: 2\nA:\n\tOther: 3\n'
        result = blocks(text)
        self.assertEqual(result['A'], 'A:\n\tHealth:\n\t\tHP: 100\nA:\n\tOther: 3')
        self.assertEqual(result['B'], 'B:\n\tTest: 2')

    def test_ignores_comment_and_blank_lines(self):
        self.assertEqual(blocks('A:\n\tX: 1\n'), blocks('# comment\nA:\n\n\t# comment\n\tX: 1\n'))

    def test_detects_added_trait(self):
        self.assertNotEqual(blocks('A:\n\tX: 1'), blocks('A:\n\tX: 1\n\tY: 2'))


if __name__ == '__main__':
    unittest.main()
