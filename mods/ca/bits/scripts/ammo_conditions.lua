Trigger.OnAnyProduction(
  function(producer, produced, productionType)
    if produced.IsTaggable
    then
      Trigger.OnPassengerEntered(produced,
        function(transport, passenger)
          if passenger.Type == 'shell.ap'
          then
            Media.DisplayMessage('add tag yes')
            transport.AddTag('yes')
          end
        end
      )
      Trigger.OnPassengerExited(produced,
        function(transport, passenger)
          transport.RemoveTag('yes')
          Media.DisplayMessage('remove tag yes')
        end
      )
    elseif produced.Type == 'chassis.mammoth'
    then
      Trigger.OnPassengerEntered(produced,
        function(transport, passenger)
          Media.DisplayMessage('check tag yes')
          if passenger.HasTag('yes')
          then
            Media.DisplayMessage('highlight')
            token = transport.GrantCondition('highlight', 0)
          end
        end
      )
      Trigger.OnPassengerExited(produced,
        function(transport, passenger)
          transport.RevokeCondition(token)
        end
      )
    end
  end
)
